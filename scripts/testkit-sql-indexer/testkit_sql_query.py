#!/usr/bin/env python3
"""
TestKit SQL Query Tool - Modular version
Интерактивный инструмент для поиска методов в TestKit через SQL базу данных
"""

import sys
import argparse
from pathlib import Path
from testkit_query import TestKitQueryEngine, QueryResult

def print_results(results: list[QueryResult], query_type: str, query_value: str):
    """Выводит результаты поиска"""
    if not results:
        print(f"No methods found for {query_type}: '{query_value}'")
        return
    
    print(f"\nFound {len(results)} methods for {query_type}: '{query_value}'")
    print("=" * 80)
    
    for i, result in enumerate(results, 1):
        print(f"{i}. {result.component_name}.{result.method_name}")
        print(f"   Return type: {result.return_type}")
        print(f"   File: {result.file_path}")
        print(f"   Line: {result.line_number}")
        if result.description:
            print(f"   Description: {result.description}")
        if result.tags:
            print(f"   Tags: {', '.join(result.tags)}")
        if result.is_static:
            print(f"   Static: Yes")
        if result.is_generic:
            print(f"   Generic: Yes")
        print()

def interactive_mode(query_engine: TestKitQueryEngine):
    """Интерактивный режим поиска"""
    print("TestKit SQL Query Tool - Interactive Mode")
    print("Available commands:")
    print("  name <method_name>     - Search by method name")
    print("  tag <tag_name>         - Search by tag")
    print("  type <return_type>     - Search by return type")
    print("  component <class_name> - Search by component/class name")
    print("  tags                   - Show all tags")
    print("  stats                  - Show database statistics")
    print("  duplicates             - Show duplicate files")
    print("  duplicate-stats        - Show duplicate statistics")
    print("  quit                   - Exit")
    print()
    
    while True:
        try:
            command = input("Query> ").strip()
            if not command:
                continue
            
            parts = command.split(maxsplit=1)
            if len(parts) == 0:
                continue
            
            cmd = parts[0].lower()
            
            if cmd == "quit":
                break
            elif cmd == "stats":
                stats = query_engine.get_statistics()
                print(f"Database Statistics:")
                print(f"  Components: {stats['total_components']}")
                print(f"  Methods: {stats['total_methods']}")
                print(f"  Tags: {stats['total_tags']}")
                print(f"  Top tags: {[tag[0] for tag in stats['top_tags'][:5]]}")
                print()
            elif cmd == "duplicates":
                duplicates = query_engine.find_duplicates()
                if duplicates:
                    print(f"Found {len(duplicates)} duplicate groups:")
                    for i, (file_hash, paths) in enumerate(duplicates[:10], 1):  # Показываем первые 10
                        print(f"  {i}. Hash: {file_hash[:16]}...")
                        for path in paths:
                            print(f"     - {path}")
                        print()
                    if len(duplicates) > 10:
                        print(f"  ... and {len(duplicates) - 10} more duplicate groups")
                else:
                    print("No duplicate files found")
                print()
            elif cmd == "duplicate-stats":
                stats = query_engine.get_duplicate_statistics()
                print(f"Duplicate Statistics:")
                print(f"  Total files: {stats['total_files']}")
                print(f"  Unique hashes: {stats['unique_hashes']}")
                print(f"  Duplicate groups: {stats['duplicate_groups']}")
                print(f"  Duplicate files: {stats['duplicate_files']}")
                print()
            elif cmd == "tags":
                tags = query_engine.get_all_tags()
                print("All tags:")
                for tag, count in tags[:20]:  # Показываем первые 20
                    print(f"  {tag}: {count} usages")
                if len(tags) > 20:
                    print(f"  ... and {len(tags) - 20} more tags")
                print()
            elif len(parts) == 2:
                value = parts[1]
                if cmd == "name":
                    results = query_engine.search_methods_by_name(value)
                    print_results(results, "method name", value)
                elif cmd == "tag":
                    results = query_engine.search_methods_by_tag(value)
                    print_results(results, "tag", value)
                elif cmd == "type":
                    results = query_engine.search_methods_by_return_type(value)
                    print_results(results, "return type", value)
                elif cmd == "component":
                    results = query_engine.search_methods_by_component(value)
                    print_results(results, "component", value)
                else:
                    print(f"Unknown command: {cmd}")
            else:
                print(f"Command '{cmd}' requires a value")
                
        except KeyboardInterrupt:
            print("\nExiting...")
            break
        except Exception as e:
            print(f"Error: {e}")

def main():
    """Main function for modular TestKit SQL Query Tool"""
    parser = argparse.ArgumentParser(description="TestKit SQL Query Tool - Modular version")
    parser.add_argument("--db-path", default="testkit_index.db", help="Database file path")
    parser.add_argument("--interactive", "-i", action="store_true", help="Start interactive mode")
    parser.add_argument("--name", help="Search by method name")
    parser.add_argument("--tag", help="Search by tag")
    parser.add_argument("--type", help="Search by return type")
    parser.add_argument("--component", help="Search by component/class name")
    parser.add_argument("--stats", action="store_true", help="Show database statistics")
    parser.add_argument("--tags", action="store_true", help="Show all tags")
    
    args = parser.parse_args()
    
    # Проверяем существование базы данных
    db_path = Path(args.db_path)
    if not db_path.exists():
        print(f"Error: Database not found: {db_path}")
        print("Please run the indexer first to create the database.")
        sys.exit(1)
    
    # Создаем движок запросов
    query_engine = TestKitQueryEngine(args.db_path)
    
    try:
        if args.interactive:
            interactive_mode(query_engine)
        elif args.stats:
            stats = query_engine.get_statistics()
            print("Database Statistics:")
            print(f"  Components: {stats['total_components']}")
            print(f"  Methods: {stats['total_methods']}")
            print(f"  Tags: {stats['total_tags']}")
            print(f"  Top tags: {[tag[0] for tag in stats['top_tags'][:10]]}")
        elif args.tags:
            tags = query_engine.get_all_tags()
            print("All tags:")
            for tag, count in tags:
                print(f"  {tag}: {count} usages")
        elif args.name:
            results = query_engine.search_methods_by_name(args.name)
            print_results(results, "method name", args.name)
        elif args.tag:
            results = query_engine.search_methods_by_tag(args.tag)
            print_results(results, "tag", args.tag)
        elif args.type:
            results = query_engine.search_methods_by_return_type(args.type)
            print_results(results, "return type", args.type)
        elif args.component:
            results = query_engine.search_methods_by_component(args.component)
            print_results(results, "component", args.component)
        else:
            # Если не указаны аргументы, запускаем интерактивный режим
            interactive_mode(query_engine)
            
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)
    finally:
        query_engine.close()

if __name__ == "__main__":
    main() 