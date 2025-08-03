#!/usr/bin/env python3
"""
Entry point for example extraction
"""

import sys
import os
from pathlib import Path

# Добавляем текущую директорию в путь
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from extract_examples import ExampleExtractor

def main():
    """Main function"""
    if len(sys.argv) < 3:
        print("Usage: python3 run_extract_examples.py <testkit_path> <tests_path> [db_path]")
        sys.exit(1)
    
    testkit_path = sys.argv[1]
    tests_path = sys.argv[2]
    db_path = sys.argv[3] if len(sys.argv) > 3 else "testkit_index.db"
    
    if not os.path.exists(testkit_path):
        print(f"TestKit path not found: {testkit_path}")
        sys.exit(1)
    
    if not os.path.exists(tests_path):
        print(f"Tests path not found: {tests_path}")
        sys.exit(1)
    
    print(f"Extracting examples from: {tests_path}")
    print(f"TestKit path: {testkit_path}")
    print(f"Database path: {db_path}")
    
    extractor = ExampleExtractor(testkit_path, tests_path, db_path)
    examples = extractor.extract_all_examples()
    
    if examples:
        extractor.save_examples(examples)
        print(f"Successfully extracted and saved {len(examples)} usage examples")
    else:
        print("No usage examples found")

if __name__ == "__main__":
    main() 