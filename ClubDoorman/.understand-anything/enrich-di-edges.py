#!/usr/bin/env python3
"""Add deterministic C# DI relationships to an understand-anything graph.

The analyzer is good at files/classes/functions but misses C# syntax-level DI.
This post-processor adds edges derived from source code, not from LLM guesses:

- implements: class X : IFoo
- depends_on: constructor/function X(...) parameter type -> known graph node
- provisions: AddSingleton/AddScoped/AddTransient/AddHostedService registrations

Dry-run by default. Pass --write to update the graph file in place.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_GRAPH = ROOT / ".understand-anything" / "knowledge-graph.json"

DECL_RE = re.compile(
    r"\b(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|record|class|interface|struct|\s)+\b"
    r"(?P<kind>class|interface|record|struct)\s+"
    r"(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
    r"(?:\s*<[^>{};()]+>)?"
    r"(?:\s*:\s*(?P<bases>[^\n{;]+))?",
    re.MULTILINE,
)
METHOD_RE = re.compile(
    r"\b(?:public|internal|private|protected|static|partial|async|virtual|override|sealed|new|extern|\s)+\b"
    r"(?:[A-Za-z_][A-Za-z0-9_<>,.\[\]?\s]*\s+)?"
    r"(?P<name>Add[A-Za-z0-9_]*)\s*\([^)]*\)\s*\{",
    re.MULTILINE,
)
DI_CALL_RE = re.compile(
    r"\.Add(?P<life>Singleton|Scoped|Transient|HostedService|KeyedSingleton|KeyedScoped|KeyedTransient)\s*"
    r"<(?P<args>[^>;]+)>\s*\(",
    re.MULTILINE,
)
PARAM_TOKEN_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_.]*(?:\s*<[^(){};]+>)?(?:\s*\[\])?")


def strip_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.DOTALL)
    return re.sub(r"//.*", "", text)


def split_type_list(value: str) -> list[str]:
    parts: list[str] = []
    start = 0
    depth = 0
    for i, ch in enumerate(value):
        if ch == "<":
            depth += 1
        elif ch == ">":
            depth = max(0, depth - 1)
        elif ch == "," and depth == 0:
            parts.append(value[start:i].strip())
            start = i + 1
    parts.append(value[start:].strip())
    return [p for p in parts if p]


def short_type(type_name: str) -> str:
    type_name = type_name.strip()
    type_name = re.sub(r"\b(in|out|ref|readonly|params)\s+", "", type_name)
    type_name = type_name.rstrip("?[]")
    if "<" in type_name:
        type_name = type_name.split("<", 1)[0]
    return type_name.rsplit(".", 1)[-1].strip()


def inner_generic_types(type_name: str) -> list[str]:
    match = re.search(r"<(?P<inner>.*)>", type_name.strip())
    if not match:
        return []
    return [short_type(p) for p in split_type_list(match.group("inner"))]


def normalized_dependency_types(type_name: str) -> list[str]:
    outer = short_type(type_name)
    inners = inner_generic_types(type_name)
    if outer in {"IEnumerable", "IReadOnlyCollection", "IReadOnlyList", "IList", "List", "ICollection", "Lazy", "Func"}:
        return [t for t in inners if t]
    if outer in {"ILogger", "ILoggerFactory", "IServiceProvider", "IServiceScopeFactory", "IOptions", "IOptionsMonitor"}:
        return []
    return [outer] if outer else []


def edge_id(source: str, target: str, edge_type: str) -> str:
    return f"e:{source}:{target}:{edge_type}"


def add_edge(edges: list[dict], seen: set[tuple[str, str, str]], source: str, target: str, edge_type: str, weight: float = 0.9) -> bool:
    key = (source, target, edge_type)
    if source == target or key in seen:
        return False
    seen.add(key)
    edges.append(
        {
            "id": edge_id(source, target, edge_type),
            "source": source,
            "target": target,
            "type": edge_type,
            "weight": weight,
            "direction": "forward",
        }
    )
    return True


def ensure_function_node(
    nodes: list[dict],
    by_file_name: dict[tuple[str, str], dict],
    by_name: dict[str, list[dict]],
    rel: str,
    name: str,
) -> tuple[dict, bool]:
    existing = by_file_name.get((rel, name))
    if existing and existing.get("type") == "function":
        return existing, False
    node = {
        "id": f"function:{rel}:{name}",
        "type": "function",
        "name": name,
        "filePath": rel,
        "summary": f"DI registration method {name} inferred from C# source.",
        "tags": ["dependency-injection", "registration", "generated"],
        "complexity": "simple",
    }
    nodes.append(node)
    by_file_name[(rel, name)] = node
    by_name.setdefault(name, []).append(node)
    return node, True


def brace_body(text: str, open_brace: int) -> str:
    depth = 0
    for i in range(open_brace, len(text)):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return text[open_brace + 1 : i]
    return text[open_brace + 1 :]


def find_constructor_params(text: str, class_name: str) -> list[str]:
    match = re.search(rf"\b(?:public|internal|private|protected)\s+{re.escape(class_name)}\s*\(", text)
    if not match:
        return []
    start = match.end()
    depth = 1
    for i in range(start, len(text)):
        if text[i] == "(":
            depth += 1
        elif text[i] == ")":
            depth -= 1
            if depth == 0:
                return extract_param_types(text[start:i])
    return []


def extract_param_types(params: str) -> list[str]:
    types: list[str] = []
    for param in split_type_list(params):
        param = re.sub(r"=\s*.*$", "", param.strip())
        param = re.sub(r"\[[^\]]+\]\s*", "", param)
        tokens = PARAM_TOKEN_RE.findall(param)
        if len(tokens) >= 2:
            types.extend(normalized_dependency_types(tokens[-2]))
    return types


def load_graph(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def write_graph(path: Path, graph: dict) -> None:
    with path.open("w", encoding="utf-8") as f:
        json.dump(graph, f, ensure_ascii=False, indent=2)
        f.write("\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--graph", type=Path, default=DEFAULT_GRAPH)
    parser.add_argument("--write", action="store_true", help="update graph file in place")
    args = parser.parse_args()

    graph = load_graph(args.graph)
    nodes = graph.get("nodes", [])
    edges = graph.setdefault("edges", [])

    by_name: dict[str, list[dict]] = {}
    by_file_name: dict[tuple[str, str], dict] = {}
    for node in nodes:
        by_name.setdefault(node.get("name", ""), []).append(node)
        file_path = node.get("filePath")
        name = node.get("name")
        if file_path and name:
            by_file_name[(file_path, name)] = node

    seen = {(e.get("source"), e.get("target"), e.get("type")) for e in edges}
    added = {"nodes": 0, "contains": 0, "implements": 0, "depends_on": 0, "provisions": 0}

    for cs_path in sorted(ROOT.rglob("*.cs")):
        if "/bin/" in cs_path.as_posix() or "/obj/" in cs_path.as_posix():
            continue
        rel = cs_path.relative_to(ROOT).as_posix()
        text = strip_comments(cs_path.read_text(encoding="utf-8", errors="ignore"))

        for decl in DECL_RE.finditer(text):
            kind = decl.group("kind")
            class_name = decl.group("name")
            node = by_file_name.get((rel, class_name))
            if not node:
                continue

            bases = decl.group("bases") or ""
            if kind != "interface":
                for base in split_type_list(bases):
                    base_name = short_type(base)
                    if not base_name.startswith("I"):
                        continue
                    for target in by_name.get(base_name, []):
                        if target.get("type") == "class" and add_edge(edges, seen, node["id"], target["id"], "implements"):
                            added["implements"] += 1

            ctor_params = find_constructor_params(text[decl.start() :], class_name)
            if ctor_params:
                source = by_file_name.get((rel, class_name), node)["id"]
                ctor = by_file_name.get((rel, class_name))
                function_ctor = next((n for n in by_name.get(class_name, []) if n.get("type") == "function" and n.get("filePath") == rel), None)
                if function_ctor:
                    source = function_ctor["id"]
                for dep_name in ctor_params:
                    for target in by_name.get(dep_name, []):
                        if target.get("type") == "class" and add_edge(edges, seen, source, target["id"], "depends_on"):
                            added["depends_on"] += 1

        for method in METHOD_RE.finditer(text):
            method_name = method.group("name")
            source_node = by_file_name.get((rel, method_name))
            if not source_node or source_node.get("type") != "function":
                source_node, created = ensure_function_node(nodes, by_file_name, by_name, rel, method_name)
                if created:
                    added["nodes"] += 1
                    file_node = by_file_name.get((rel, Path(rel).name))
                    if file_node and add_edge(edges, seen, file_node["id"], source_node["id"], "contains", 1.0):
                        added["contains"] += 1
            body = brace_body(text, method.end() - 1)
            for call in DI_CALL_RE.finditer(body):
                type_args = [short_type(arg) for arg in split_type_list(call.group("args"))]
                if not type_args:
                    continue
                targets = type_args[-1:] if len(type_args) > 1 else type_args
                for target_name in targets:
                    for target in by_name.get(target_name, []):
                        if target.get("type") == "class" and add_edge(edges, seen, source_node["id"], target["id"], "provisions"):
                            added["provisions"] += 1

    print(json.dumps({"graph": str(args.graph), "added": added, "totalEdges": len(edges), "write": args.write}, ensure_ascii=False, indent=2))
    if args.write:
        write_graph(args.graph, graph)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
