#!/usr/bin/env python3
"""
Test script for deduplication with detailed timing
"""

import time
import sys
from pathlib import Path
from testkit_indexer import TestKitIndexer
from testkit_indexer.deduplicator import DeduplicationAnalyzer, DeduplicationEngine
from typing import List, Dict
from testkit_indexer.models import TestKitComponent

class TimedDeduplicationAnalyzer(DeduplicationAnalyzer):
    """Анализатор с детальным трейсингом времени"""
    
    def __init__(self):
        super().__init__()
        self.timings = {}
    
    def analyze_duplicates(self, components: List[TestKitComponent]) -> Dict:
        """Анализирует дубликаты с использованием всех стратегий"""
        analysis = {}
        
        for strategy_name, strategy in self.analyzers.items():
            print(f"\n--- Analyzing {strategy_name} strategy ---")
            
            start_time = time.time()
            
            # Создаем движок
            engine_start = time.time()
            engine = DeduplicationEngine(strategy)
            engine_time = time.time() - engine_start
            print(f"  Engine creation: {engine_time:.4f}s")
            
            # Выполняем дедупликацию
            dedup_start = time.time()
            unique_components = engine.deduplicate(components.copy())
            dedup_time = time.time() - dedup_start
            print(f"  Deduplication: {dedup_time:.4f}s")
            
            total_time = time.time() - start_time
            
            analysis[strategy_name] = {
                'original_count': len(components),
                'unique_count': len(unique_components),
                'removed_count': len(components) - len(unique_components),
                'duplicate_report': engine.get_duplicate_report(),
                'timing': {
                    'engine_creation': engine_time,
                    'deduplication': dedup_time,
                    'total': total_time
                }
            }
            
            self.timings[strategy_name] = analysis[strategy_name]['timing']
        
        return analysis
    
    def print_timing_analysis(self):
        """Выводит детальный анализ времени"""
        print("\n" + "="*60)
        print("DETAILED TIMING ANALYSIS")
        print("="*60)
        
        for strategy_name, timing in self.timings.items():
            print(f"\n{strategy_name.upper()} Strategy:")
            print(f"  Engine creation: {timing['engine_creation']:.4f}s")
            print(f"  Deduplication:   {timing['deduplication']:.4f}s")
            print(f"  Total:           {timing['total']:.4f}s")

def test_deduplication_with_timing():
    """Тестирует дедупликацию с детальным трейсингом"""
    print("Testing deduplication with detailed timing...")
    
    # Путь к TestKit
    testkit_path = "../../ClubDoorman.Test/TestKit"
    
    if not Path(testkit_path).exists():
        print(f"Error: TestKit path not found: {testkit_path}")
        return
    
    # Создаем индексер
    indexer = TestKitIndexer(testkit_path, "test_timing.db")
    
    # Сканируем файлы
    print("Scanning TestKit files...")
    scan_start = time.time()
    components = indexer.scanner.scan_testkit()
    scan_time = time.time() - scan_start
    
    if not components:
        print("No components found!")
        return
    
    print(f"Found {len(components)} components in {scan_time:.4f}s")
    
    # Анализируем дубликаты с трейсингом
    analyzer = TimedDeduplicationAnalyzer()
    analysis = analyzer.analyze_duplicates(components)
    
    # Выводим результаты
    analyzer.print_analysis(analysis)
    analyzer.print_timing_analysis()
    
    return analysis

def test_individual_strategy_timing():
    """Тестирует каждую стратегию отдельно с детальным трейсингом"""
    print("\n" + "="*60)
    print("INDIVIDUAL STRATEGY TIMING")
    print("="*60)
    
    testkit_path = "../../ClubDoorman.Test/TestKit"
    
    if not Path(testkit_path).exists():
        print(f"Error: TestKit path not found: {testkit_path}")
        return
    
    # Сканируем файлы один раз
    indexer = TestKitIndexer(testkit_path, "test_individual.db")
    components = indexer.scanner.scan_testkit()
    
    if not components:
        print("No components found!")
        return
    
    strategies = ["file_hash", "signature", "content"]
    
    for strategy in strategies:
        print(f"\n--- Testing {strategy} strategy in detail ---")
        
        # Создаем индексер с конкретной стратегией
        indexer = TestKitIndexer(testkit_path, f"test_{strategy}_detail.db", strategy)
        
        # Тестируем только дедупликацию
        dedup_start = time.time()
        unique_components = indexer._deduplicate_components(components)
        dedup_time = time.time() - dedup_start
        
        print(f"  Original: {len(components)} components")
        print(f"  After deduplication: {len(unique_components)} components")
        print(f"  Removed: {len(components) - len(unique_components)} components")
        print(f"  Time: {dedup_time:.4f}s")
        print(f"  Time per component: {dedup_time/len(components):.6f}s")

def main():
    """Main function"""
    print("TestKit Deduplication Timing Test")
    print("=" * 60)
    
    # Тест с детальным трейсингом
    analysis = test_deduplication_with_timing()
    
    # Тест каждой стратегии отдельно
    test_individual_strategy_timing()
    
    print("\n" + "=" * 60)
    print("Timing test completed!")

if __name__ == "__main__":
    main() 