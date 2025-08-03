#!/usr/bin/env python3
"""
Main indexer module for TestKit SQL Indexer
"""

import sys
from pathlib import Path
from typing import List, Optional
from .models import TestKitComponent
from .scanner import TestKitScanner
from .database import DatabaseManager
from .reporter import ReportGenerator

class TestKitIndexer:
    """Main indexer class that orchestrates the indexing process"""
    
    def __init__(self, testkit_path: str, db_path: str = "testkit_index.db"):
        self.testkit_path = testkit_path
        self.db_path = db_path
        self.scanner = TestKitScanner(testkit_path)
        self.db_manager = DatabaseManager(db_path)
        self.reporter = ReportGenerator()
        self.components: List[TestKitComponent] = []
    
    def run_indexing(self, generate_reports: bool = True) -> dict:
        """Запускает полный процесс индексации"""
        print("Starting TestKit SQL Indexer...")
        print(f"TestKit path: {self.testkit_path}")
        print(f"Database path: {self.db_path}")
        print()
        
        # Сканируем файлы
        print("Scanning TestKit files...")
        self.components = self.scanner.scan_testkit()
        
        if not self.components:
            print("No components found!")
            return {}
        
        # Создаем базу данных
        print("Creating database...")
        self.db_manager.create_database()
        
        # Сохраняем компоненты в БД
        print("Saving components to database...")
        self.db_manager.save_components(self.components)
        
        # Получаем статистику
        print("Generating statistics...")
        db_stats = self.db_manager.get_statistics()
        scan_stats = self.scanner.get_statistics()
        
        # Генерируем отчеты
        if generate_reports:
            print("Generating reports...")
            self._generate_reports(db_stats)
        
        # Закрываем соединение с БД
        self.db_manager.close()
        
        print("Indexing completed successfully!")
        return {
            'components': len(self.components),
            'methods': scan_stats['total_methods'],
            'tags': scan_stats['total_tags'],
            'db_stats': db_stats
        }
    
    def _generate_reports(self, db_stats: dict):
        """Генерирует отчеты"""
        # Сводный отчет
        summary_report = self.reporter.generate_summary_report(self.components, db_stats)
        self.reporter.save_report_to_file(summary_report, "testkit_index_summary.txt")
        
        # Детальный отчет
        detailed_report = self.reporter.generate_detailed_report(self.components)
        self.reporter.save_report_to_file(detailed_report, "testkit_index_detailed.txt")
        
        # Отчет по тегам
        tag_report = self.reporter.generate_tag_report(self.components)
        self.reporter.save_report_to_file(tag_report, "testkit_index_tags.txt")
    
    def get_components(self) -> List[TestKitComponent]:
        """Возвращает проанализированные компоненты"""
        return self.components
    
    def get_statistics(self) -> dict:
        """Возвращает статистику индексации"""
        if not self.components:
            return {}
        
        scan_stats = self.scanner.get_statistics()
        db_stats = self.db_manager.get_statistics() if self.db_manager.conn else {}
        
        return {
            'scan_stats': scan_stats,
            'db_stats': db_stats,
            'components_count': len(self.components)
        } 