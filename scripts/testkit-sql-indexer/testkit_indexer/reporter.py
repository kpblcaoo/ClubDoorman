#!/usr/bin/env python3
"""
Report generator for TestKit SQL Indexer
"""

from datetime import datetime
from typing import Dict, List
from .models import TestKitComponent

class ReportGenerator:
    """Generates reports and statistics for TestKit index"""
    
    def __init__(self):
        self.generated_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    
    def generate_summary_report(self, components: List[TestKitComponent], db_stats: Dict) -> str:
        """Генерирует сводный отчет"""
        report = []
        report.append("=" * 80)
        report.append("TESTKIT SQL INDEXER - SUMMARY REPORT")
        report.append("=" * 80)
        report.append(f"Generated at: {self.generated_at}")
        report.append("")
        
        # Общая статистика
        total_methods = sum(len(c.methods) for c in components)
        total_tags = len(set(tag for c in components for m in c.methods for tag in m.tags))
        
        report.append("GENERAL STATISTICS:")
        report.append(f"  Total components: {len(components)}")
        report.append(f"  Total methods: {total_methods}")
        report.append(f"  Total unique tags: {total_tags}")
        report.append("")
        
        # Статистика из БД
        if db_stats:
            report.append("DATABASE STATISTICS:")
            report.append(f"  Components in DB: {db_stats.get('total_components', 0)}")
            report.append(f"  Methods in DB: {db_stats.get('total_methods', 0)}")
            report.append(f"  Tags in DB: {db_stats.get('total_tags', 0)}")
            report.append("")
        
        # Топ категорий
        category_counts = {}
        for component in components:
            category = component.category
            category_counts[category] = category_counts.get(category, 0) + 1
        
        report.append("TOP CATEGORIES:")
        for category, count in sorted(category_counts.items(), key=lambda x: x[1], reverse=True)[:10]:
            report.append(f"  {category}: {count} components")
        report.append("")
        
        # Топ тегов
        tag_counts = {}
        for component in components:
            for method in component.methods:
                for tag in method.tags:
                    tag_counts[tag] = tag_counts.get(tag, 0) + 1
        
        report.append("TOP TAGS:")
        for tag, count in sorted(tag_counts.items(), key=lambda x: x[1], reverse=True)[:15]:
            report.append(f"  {tag}: {count} usages")
        report.append("")
        
        # Топ методов по количеству тегов
        method_tag_counts = []
        for component in components:
            for method in component.methods:
                method_tag_counts.append((f"{component.class_name}.{method.name}", len(method.tags)))
        
        report.append("METHODS WITH MOST TAGS:")
        for method_name, tag_count in sorted(method_tag_counts, key=lambda x: x[1], reverse=True)[:10]:
            report.append(f"  {method_name}: {tag_count} tags")
        report.append("")
        
        return "\n".join(report)
    
    def generate_detailed_report(self, components: List[TestKitComponent]) -> str:
        """Генерирует детальный отчет по компонентам"""
        report = []
        report.append("=" * 80)
        report.append("TESTKIT SQL INDEXER - DETAILED REPORT")
        report.append("=" * 80)
        report.append(f"Generated at: {self.generated_at}")
        report.append("")
        
        # Группируем по категориям
        categories = {}
        for component in components:
            category = component.category
            if category not in categories:
                categories[category] = []
            categories[category].append(component)
        
        for category, category_components in sorted(categories.items()):
            report.append(f"CATEGORY: {category}")
            report.append("-" * 50)
            report.append(f"Components: {len(category_components)}")
            report.append("")
            
            for component in category_components:
                report.append(f"  Component: {component.class_name}")
                report.append(f"    File: {component.file_name}")
                report.append(f"    Path: {component.file_path}")
                report.append(f"    Lines: {component.lines_count}")
                if component.class_description:
                    report.append(f"    Description: {component.class_description}")
                report.append(f"    Methods: {len(component.methods)}")
                
                # Показываем методы с тегами
                for method in component.methods:
                    tags_str = ", ".join(method.tags) if method.tags else "no tags"
                    report.append(f"      - {method.name} ({method.return_type}): {tags_str}")
                report.append("")
        
        return "\n".join(report)
    
    def generate_tag_report(self, components: List[TestKitComponent]) -> str:
        """Генерирует отчет по тегам"""
        report = []
        report.append("=" * 80)
        report.append("TESTKIT SQL INDEXER - TAG ANALYSIS")
        report.append("=" * 80)
        report.append(f"Generated at: {self.generated_at}")
        report.append("")
        
        # Собираем статистику по тегам
        tag_stats = {}
        for component in components:
            for method in component.methods:
                for tag in method.tags:
                    if tag not in tag_stats:
                        tag_stats[tag] = {
                            'count': 0,
                            'methods': [],
                            'components': set()
                        }
                    tag_stats[tag]['count'] += 1
                    tag_stats[tag]['methods'].append(f"{component.class_name}.{method.name}")
                    tag_stats[tag]['components'].add(component.class_name)
        
        # Сортируем по количеству использований
        sorted_tags = sorted(tag_stats.items(), key=lambda x: x[1]['count'], reverse=True)
        
        report.append("TAG USAGE STATISTICS:")
        report.append("")
        
        for tag, stats in sorted_tags:
            report.append(f"TAG: {tag}")
            report.append(f"  Total usages: {stats['count']}")
            report.append(f"  Unique components: {len(stats['components'])}")
            report.append(f"  Methods: {', '.join(stats['methods'][:5])}")
            if len(stats['methods']) > 5:
                report.append(f"  ... and {len(stats['methods']) - 5} more")
            report.append("")
        
        return "\n".join(report)
    
    def save_report_to_file(self, report: str, filename: str):
        """Сохраняет отчет в файл"""
        with open(filename, 'w', encoding='utf-8') as f:
            f.write(report)
        print(f"Report saved to: {filename}") 