#!/usr/bin/env python3
"""
ClubDoorman Architectural Issues Detection Script
Analyzes C# codebase for critical architectural problems
"""

import os
import re
import json
from pathlib import Path
from collections import defaultdict, Counter
from typing import Dict, List, Set, Tuple
from dataclasses import dataclass

@dataclass
class FileMetrics:
    """Metrics for a single C# file"""
    path: str
    lines: int
    classes: int
    methods: int
    dependencies: int
    complexity_score: float

@dataclass
class ArchitecturalIssue:
    """Represents an architectural issue found in the code"""
    severity: str  # CRITICAL, HIGH, MEDIUM, LOW
    category: str  # SRP_VIOLATION, COUPLING, COMPLEXITY, etc.
    description: str
    file_path: str
    line_number: int
    recommendation: str

class ArchitecturalAnalyzer:
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.issues: List[ArchitecturalIssue] = []
        self.file_metrics: Dict[str, FileMetrics] = {}
        
    def analyze(self) -> Dict:
        """Run full architectural analysis"""
        print("🔍 Starting architectural analysis...")
        
        # Find all C# files
        cs_files = list(self.project_root.glob("**/*.cs"))
        cs_files = [f for f in cs_files if not any(excluded in str(f) for excluded in ['.git', 'bin', 'obj', 'TestKit'])]
        
        print(f"📁 Found {len(cs_files)} C# files to analyze")
        
        # Analyze each file
        for cs_file in cs_files:
            self._analyze_file(cs_file)
            
        # Detect architectural issues
        self._detect_srp_violations()
        self._detect_high_coupling()
        self._detect_complex_methods()
        self._detect_long_parameter_lists()
        self._detect_god_classes()
        self._detect_feature_envy()
        
        return self._generate_report()
        
    def _analyze_file(self, file_path: Path):
        """Analyze a single C# file"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                
            relative_path = str(file_path.relative_to(self.project_root))
            
            # Basic metrics
            lines = len(content.splitlines())
            classes = len(re.findall(r'^\s*public\s+class\s+\w+', content, re.MULTILINE))
            methods = len(re.findall(r'^\s*public\s+.*?\s+\w+\s*\([^)]*\)', content, re.MULTILINE))
            dependencies = len(re.findall(r'using\s+[\w.]+;', content))
            
            # Complexity score (simplified)
            complexity_score = self._calculate_complexity(content)
            
            self.file_metrics[relative_path] = FileMetrics(
                path=relative_path,
                lines=lines,
                classes=classes,
                methods=methods,
                dependencies=dependencies,
                complexity_score=complexity_score
            )
            
        except Exception as e:
            print(f"⚠️  Error analyzing {file_path}: {e}")
            
    def _calculate_complexity(self, content: str) -> float:
        """Calculate complexity score for file content"""
        score = 0
        
        # Cyclomatic complexity indicators
        score += len(re.findall(r'\bif\s*\(', content)) * 1
        score += len(re.findall(r'\belse\b', content)) * 1
        score += len(re.findall(r'\bfor\s*\(', content)) * 2
        score += len(re.findall(r'\bwhile\s*\(', content)) * 2
        score += len(re.findall(r'\bswitch\s*\(', content)) * 3
        score += len(re.findall(r'\bcatch\s*\(', content)) * 2
        score += len(re.findall(r'\btry\s*{', content)) * 1
        
        # Nested complexity
        nesting_level = content.count('{') - content.count('}')
        score += abs(nesting_level) * 0.5
        
        return score
        
    def _detect_srp_violations(self):
        """Detect Single Responsibility Principle violations"""
        for path, metrics in self.file_metrics.items():
            if metrics.lines > 500:
                severity = "CRITICAL" if metrics.lines > 800 else "HIGH"
                self.issues.append(ArchitecturalIssue(
                    severity=severity,
                    category="SRP_VIOLATION",
                    description=f"File too large: {metrics.lines} lines (should be <300)",
                    file_path=path,
                    line_number=1,
                    recommendation=f"Split into {metrics.lines // 300 + 1} smaller classes with single responsibilities"
                ))
                
            if metrics.methods > 20:
                self.issues.append(ArchitecturalIssue(
                    severity="HIGH",
                    category="SRP_VIOLATION", 
                    description=f"Too many methods: {metrics.methods} (should be <15)",
                    file_path=path,
                    line_number=1,
                    recommendation="Extract related methods into separate classes or use composition"
                ))
                
    def _detect_high_coupling(self):
        """Detect high coupling issues"""
        for path, metrics in self.file_metrics.items():
            if metrics.dependencies > 15:
                self.issues.append(ArchitecturalIssue(
                    severity="HIGH",
                    category="HIGH_COUPLING",
                    description=f"Too many dependencies: {metrics.dependencies} using statements",
                    file_path=path,
                    line_number=1,
                    recommendation="Reduce dependencies by using interfaces and dependency injection"
                ))
                
    def _detect_complex_methods(self):
        """Detect overly complex methods"""
        for path, metrics in self.file_metrics.items():
            if metrics.complexity_score > 50:
                severity = "CRITICAL" if metrics.complexity_score > 100 else "HIGH"
                self.issues.append(ArchitecturalIssue(
                    severity=severity,
                    category="HIGH_COMPLEXITY",
                    description=f"High complexity score: {metrics.complexity_score:.1f}",
                    file_path=path,
                    line_number=1,
                    recommendation="Break down complex methods, reduce nesting, use guard clauses"
                ))
                
    def _detect_long_parameter_lists(self):
        """Detect methods with too many parameters"""
        for path, metrics in self.file_metrics.items():
            try:
                with open(self.project_root / path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                # Find method signatures with many parameters
                method_pattern = r'public\s+.*?\s+(\w+)\s*\(([^)]*)\)'
                methods = re.findall(method_pattern, content, re.MULTILINE | re.DOTALL)
                
                for method_name, params in methods:
                    param_count = len([p for p in params.split(',') if p.strip()])
                    if param_count > 5:
                        self.issues.append(ArchitecturalIssue(
                            severity="MEDIUM",
                            category="LONG_PARAMETER_LIST",
                            description=f"Method {method_name} has {param_count} parameters (should be ≤5)",
                            file_path=path,
                            line_number=1,
                            recommendation="Use parameter objects or builder pattern"
                        ))
                        
            except Exception:
                pass
                
    def _detect_god_classes(self):
        """Detect God Classes (classes that do too much)"""
        for path, metrics in self.file_metrics.items():
            # God class indicators
            is_god_class = (
                metrics.lines > 600 and 
                metrics.methods > 15 and 
                metrics.dependencies > 10
            )
            
            if is_god_class:
                self.issues.append(ArchitecturalIssue(
                    severity="CRITICAL",
                    category="GOD_CLASS",
                    description=f"God class detected: {metrics.lines} lines, {metrics.methods} methods, {metrics.dependencies} dependencies",
                    file_path=path,
                    line_number=1,
                    recommendation="Extract responsibilities into separate classes using Single Responsibility Principle"
                ))
                
    def _detect_feature_envy(self):
        """Detect Feature Envy (methods using too much from other classes)"""
        # Simplified detection - look for files with many external method calls
        for path, metrics in self.file_metrics.items():
            try:
                with open(self.project_root / path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                # Count external method calls (simplified heuristic)
                external_calls = len(re.findall(r'\w+\.\w+\([^)]*\)', content))
                if external_calls > 30 and metrics.lines < 300:
                    self.issues.append(ArchitecturalIssue(
                        severity="MEDIUM",
                        category="FEATURE_ENVY",
                        description=f"High number of external method calls: {external_calls}",
                        file_path=path,
                        line_number=1,
                        recommendation="Consider moving methods closer to the data they use"
                    ))
                    
            except Exception:
                pass
                
    def _generate_report(self) -> Dict:
        """Generate comprehensive analysis report"""
        issues_by_severity = defaultdict(list)
        issues_by_category = defaultdict(list)
        
        for issue in self.issues:
            issues_by_severity[issue.severity].append(issue)
            issues_by_category[issue.category].append(issue)
            
        # File rankings
        largest_files = sorted(self.file_metrics.items(), key=lambda x: x[1].lines, reverse=True)[:10]
        most_complex = sorted(self.file_metrics.items(), key=lambda x: x[1].complexity_score, reverse=True)[:10]
        
        return {
            "summary": {
                "total_files": len(self.file_metrics),
                "total_issues": len(self.issues),
                "critical_issues": len(issues_by_severity["CRITICAL"]),
                "high_issues": len(issues_by_severity["HIGH"]),
                "medium_issues": len(issues_by_severity["MEDIUM"]),
                "average_file_size": sum(m.lines for m in self.file_metrics.values()) / len(self.file_metrics),
                "average_complexity": sum(m.complexity_score for m in self.file_metrics.values()) / len(self.file_metrics)
            },
            "issues_by_severity": {k: [self._issue_to_dict(i) for i in v] for k, v in issues_by_severity.items()},
            "issues_by_category": {k: len(v) for k, v in issues_by_category.items()},
            "largest_files": [(path, metrics.lines) for path, metrics in largest_files],
            "most_complex_files": [(path, metrics.complexity_score) for path, metrics in most_complex],
            "recommendations": self._generate_recommendations(issues_by_category)
        }
        
    def _issue_to_dict(self, issue: ArchitecturalIssue) -> Dict:
        """Convert issue to dictionary"""
        return {
            "severity": issue.severity,
            "category": issue.category,
            "description": issue.description,
            "file_path": issue.file_path,
            "line_number": issue.line_number,
            "recommendation": issue.recommendation
        }
        
    def _generate_recommendations(self, issues_by_category: Dict) -> List[str]:
        """Generate prioritized recommendations"""
        recommendations = []
        
        if "GOD_CLASS" in issues_by_category:
            recommendations.append("🚨 CRITICAL: Break down God Classes using Extract Class refactoring")
            
        if "SRP_VIOLATION" in issues_by_category:
            recommendations.append("🔴 HIGH: Address Single Responsibility Principle violations")
            
        if "HIGH_COUPLING" in issues_by_category:
            recommendations.append("🔶 HIGH: Reduce coupling through dependency injection and interfaces")
            
        if "HIGH_COMPLEXITY" in issues_by_category:
            recommendations.append("🔶 HIGH: Simplify complex methods using Extract Method refactoring")
            
        if "LONG_PARAMETER_LIST" in issues_by_category:
            recommendations.append("🔷 MEDIUM: Use parameter objects for methods with many parameters")
            
        if "FEATURE_ENVY" in issues_by_category:
            recommendations.append("🔷 MEDIUM: Move methods closer to the data they use")
            
        return recommendations

def main():
    analyzer = ArchitecturalAnalyzer(".")
    report = analyzer.analyze()
    
    print("\n" + "="*60)
    print("🏗️  ARCHITECTURAL ANALYSIS REPORT")
    print("="*60)
    
    summary = report["summary"]
    print(f"\n📊 Summary:")
    print(f"   Files Analyzed: {summary['total_files']}")
    print(f"   Total Issues: {summary['total_issues']}")
    print(f"   🚨 Critical: {summary['critical_issues']}")
    print(f"   🔴 High: {summary['high_issues']}")
    print(f"   🔶 Medium: {summary['medium_issues']}")
    print(f"   Average File Size: {summary['average_file_size']:.1f} lines")
    print(f"   Average Complexity: {summary['average_complexity']:.1f}")
    
    print(f"\n🗂️  Largest Files:")
    for path, lines in report["largest_files"][:5]:
        print(f"   📄 {path}: {lines} lines")
        
    print(f"\n🌀 Most Complex Files:")
    for path, complexity in report["most_complex_files"][:5]:
        print(f"   🔥 {path}: {complexity:.1f} complexity")
        
    print(f"\n📋 Issues by Category:")
    for category, count in report["issues_by_category"].items():
        print(f"   {category}: {count} issues")
        
    print(f"\n💡 Recommendations:")
    for rec in report["recommendations"]:
        print(f"   {rec}")
        
    # Save detailed report to file
    with open("architectural_analysis_report.json", "w") as f:
        json.dump(report, f, indent=2)
        
    print(f"\n📄 Detailed report saved to: architectural_analysis_report.json")
    
    # Return exit code based on critical issues
    return 1 if summary['critical_issues'] > 0 else 0

if __name__ == "__main__":
    exit(main())