#!/usr/bin/env python3
"""
Test script for duplicate statistics
"""

from testkit_query import TestKitQueryEngine

def main():
    """Main function"""
    print("Testing duplicate statistics...")
    
    # Создаем query engine
    query_engine = TestKitQueryEngine("testkit_with_deduplication.db")
    
    try:
        # Получаем статистику дубликатов
        stats = query_engine.get_duplicate_statistics()
        print(f"Duplicate Statistics:")
        print(f"  Total files: {stats['total_files']}")
        print(f"  Unique hashes: {stats['unique_hashes']}")
        print(f"  Duplicate groups: {stats['duplicate_groups']}")
        print(f"  Duplicate files: {stats['duplicate_files']}")
        
        # Находим дубликаты
        duplicates = query_engine.find_duplicates()
        if duplicates:
            print(f"\nFound {len(duplicates)} duplicate groups:")
            for i, (file_hash, paths) in enumerate(duplicates, 1):
                print(f"  {i}. Hash: {file_hash[:16]}...")
                for path in paths:
                    print(f"     - {path}")
                print()
        else:
            print("\nNo duplicate files found")
            
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    main() 