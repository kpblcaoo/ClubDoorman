#!/usr/bin/env python3
"""
Entry point for TestKit indexer
"""

import sys
import os
from pathlib import Path

# Добавляем текущую директорию в путь
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from testkit_indexer.indexer import TestKitIndexer

def main():
    """Main function"""
    if len(sys.argv) < 3:
        print("Usage: python3 run_indexer.py <testkit_path> <db_path>")
        sys.exit(1)
    
    testkit_path = sys.argv[1]
    db_path = sys.argv[2]
    
    if not os.path.exists(testkit_path):
        print(f"TestKit path not found: {testkit_path}")
        sys.exit(1)
    
    print(f"Indexing TestKit at: {testkit_path}")
    print(f"Database path: {db_path}")
    
    indexer = TestKitIndexer(testkit_path, db_path)
    result = indexer.run_indexing()
    
    print(f"Indexing completed: {result}")

if __name__ == "__main__":
    main() 