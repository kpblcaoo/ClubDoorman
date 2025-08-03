#!/usr/bin/env python3
"""
TestKit SQL Index Generator - Modular version
Анализирует все файлы, методы, комментарии и создает структурированную БД для быстрого поиска
"""

import sys
import argparse
from pathlib import Path
from testkit_indexer import TestKitIndexer

def main():
    """Main function for modular TestKit SQL Indexer"""
    parser = argparse.ArgumentParser(description="TestKit SQL Indexer - Modular version")
    parser.add_argument("testkit_path", help="Path to TestKit directory")
    parser.add_argument("--db-path", default="testkit_index.db", help="Database file path")
    parser.add_argument("--no-reports", action="store_true", help="Skip report generation")
    parser.add_argument("--no-deduplicate", action="store_true", help="Skip duplicate file removal")
    parser.add_argument("--deduplication-strategy", 
                       choices=["file_hash", "signature", "content", "none"],
                       default="file_hash",
                       help="Deduplication strategy to use")
    
    args = parser.parse_args()
    
    # Проверяем путь к TestKit
    testkit_path = Path(args.testkit_path)
    if not testkit_path.exists():
        print(f"Error: TestKit path not found: {testkit_path}")
        sys.exit(1)
    
    # Создаем индексер и запускаем процесс
    deduplication_strategy = "none" if args.no_deduplicate else args.deduplication_strategy
    indexer = TestKitIndexer(str(testkit_path), args.db_path, deduplication_strategy)
    
    try:
        result = indexer.run_indexing(generate_reports=not args.no_reports, deduplicate=not args.no_deduplicate)
        
        if result:
            print("\n" + "="*50)
            print("INDEXING COMPLETED SUCCESSFULLY")
            print("="*50)
            print(f"Components analyzed: {result['components']}")
            print(f"Methods found: {result['methods']}")
            print(f"Unique tags: {result['tags']}")
            print(f"Database: {args.db_path}")
            
            if not args.no_reports:
                print("\nReports generated:")
                print("  - testkit_index_summary.txt")
                print("  - testkit_index_detailed.txt")
                print("  - testkit_index_tags.txt")
        else:
            print("No components were analyzed!")
            sys.exit(1)
            
    except Exception as e:
        print(f"Error during indexing: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main() 