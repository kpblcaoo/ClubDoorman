#!/usr/bin/env python3
"""
Test script for deduplication functionality
"""

import sys
from pathlib import Path
from testkit_indexer import TestKitIndexer
from testkit_indexer.deduplicator import DeduplicationAnalyzer

def test_deduplication_analysis():
    """Тестирует анализ дубликатов"""
    print("Testing deduplication analysis...")
    
    # Путь к TestKit
    testkit_path = "../../ClubDoorman.Test/TestKit"
    
    if not Path(testkit_path).exists():
        print(f"Error: TestKit path not found: {testkit_path}")
        return
    
    # Создаем индексер
    indexer = TestKitIndexer(testkit_path, "test_deduplication.db")
    
    # Сканируем файлы
    print("Scanning TestKit files...")
    components = indexer.scanner.scan_testkit()
    
    if not components:
        print("No components found!")
        return
    
    print(f"Found {len(components)} components")
    
    # Анализируем дубликаты
    analyzer = DeduplicationAnalyzer()
    analysis = analyzer.analyze_duplicates(components)
    
    # Выводим результаты
    analyzer.print_analysis(analysis)
    
    return analysis

def test_deduplication_strategies():
    """Тестирует различные стратегии дедупликации"""
    print("\nTesting different deduplication strategies...")
    
    testkit_path = "../../ClubDoorman.Test/TestKit"
    
    if not Path(testkit_path).exists():
        print(f"Error: TestKit path not found: {testkit_path}")
        return
    
    strategies = ["file_hash", "signature", "content"]
    
    for strategy in strategies:
        print(f"\n--- Testing {strategy} strategy ---")
        
        # Создаем индексер с конкретной стратегией
        indexer = TestKitIndexer(testkit_path, f"test_{strategy}.db", strategy)
        
        # Сканируем файлы
        components = indexer.scanner.scan_testkit()
        
        if not components:
            print("No components found!")
            continue
        
        # Выполняем дедупликацию
        unique_components = indexer._deduplicate_components(components)
        
        print(f"Original: {len(components)} components")
        print(f"After deduplication: {len(unique_components)} components")
        print(f"Removed: {len(components) - len(unique_components)} components")

def main():
    """Main function"""
    print("TestKit Deduplication Test")
    print("=" * 40)
    
    # Тест анализа дубликатов
    analysis = test_deduplication_analysis()
    
    # Тест различных стратегий
    test_deduplication_strategies()
    
    print("\n" + "=" * 40)
    print("Deduplication test completed!")

if __name__ == "__main__":
    main() 