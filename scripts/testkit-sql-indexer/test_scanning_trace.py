#!/usr/bin/env python3
"""
Test script for scanning with detailed timing
"""

import time
import sys
from pathlib import Path
from testkit_indexer import TestKitIndexer

def test_scanning_with_timing():
    """Тестирует сканирование с детальным трейсингом"""
    print("Testing scanning with detailed timing...")
    
    # Путь к TestKit
    testkit_path = "../../ClubDoorman.Test/TestKit"
    
    if not Path(testkit_path).exists():
        print(f"Error: TestKit path not found: {testkit_path}")
        return
    
    # Создаем индексер
    indexer = TestKitIndexer(testkit_path, "test_scanning.db")
    
    # Сканируем файлы с детальным трейсингом
    print("Scanning TestKit files...")
    
    # Находим все .cs файлы
    testkit_path_obj = Path(testkit_path)
    cs_files = list(testkit_path_obj.rglob("*.cs"))
    print(f"Found {len(cs_files)} C# files")
    
    # Анализируем каждый файл отдельно
    components = []
    total_parse_time = 0
    total_hash_time = 0
    
    for i, file_path in enumerate(cs_files):
        print(f"  Processing {i+1}/{len(cs_files)}: {file_path.name}")
        
        # Время парсинга
        parse_start = time.time()
        component = indexer.scanner.analyze_file(file_path)
        parse_time = time.time() - parse_start
        total_parse_time += parse_time
        
        if component:
            # Время вычисления хеша
            hash_start = time.time()
            file_hash = indexer.scanner._calculate_file_hash(file_path)
            hash_time = time.time() - hash_start
            total_hash_time += hash_time
            
            component.file_hash = file_hash
            components.append(component)
            
            print(f"    Parse: {parse_time:.4f}s, Hash: {hash_time:.4f}s, Methods: {len(component.methods)}")
        else:
            print(f"    Parse: {parse_time:.4f}s, Hash: 0.0000s, Methods: 0 (failed)")
    
    print(f"\nTotal scanning time: {total_parse_time + total_hash_time:.4f}s")
    print(f"  Parse time: {total_parse_time:.4f}s")
    print(f"  Hash time: {total_hash_time:.4f}s")
    print(f"  Components: {len(components)}")
    
    return components

def test_individual_file_timing():
    """Тестирует время обработки отдельных файлов"""
    print("\n" + "="*60)
    print("INDIVIDUAL FILE TIMING")
    print("="*60)
    
    testkit_path = "../../ClubDoorman.Test/TestKit"
    
    if not Path(testkit_path).exists():
        print(f"Error: TestKit path not found: {testkit_path}")
        return
    
    # Создаем индексер
    indexer = TestKitIndexer(testkit_path, "test_individual_files.db")
    
    # Находим все .cs файлы
    testkit_path_obj = Path(testkit_path)
    cs_files = list(testkit_path_obj.rglob("*.cs"))
    
    # Сортируем по размеру файла
    file_sizes = []
    for file_path in cs_files:
        try:
            size = file_path.stat().st_size
            file_sizes.append((file_path, size))
        except:
            file_sizes.append((file_path, 0))
    
    # Сортируем по размеру (от большего к меньшему)
    file_sizes.sort(key=lambda x: x[1], reverse=True)
    
    print("Top 10 largest files:")
    for i, (file_path, size) in enumerate(file_sizes[:10]):
        print(f"  {i+1}. {file_path.name} ({size} bytes)")
        
        # Тестируем время обработки
        start_time = time.time()
        component = indexer.scanner.analyze_file(file_path)
        parse_time = time.time() - start_time
        
        if component:
            print(f"     Parse: {parse_time:.4f}s, Methods: {len(component.methods)}")
        else:
            print(f"     Parse: {parse_time:.4f}s, Methods: 0 (failed)")

def main():
    """Main function"""
    print("TestKit Scanning Timing Test")
    print("=" * 60)
    
    # Тест сканирования с трейсингом
    components = test_scanning_with_timing()
    
    # Тест отдельных файлов
    test_individual_file_timing()
    
    print("\n" + "=" * 60)
    print("Scanning timing test completed!")

if __name__ == "__main__":
    main() 