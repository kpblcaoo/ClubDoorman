#!/usr/bin/env python3
"""
C# file parser for TestKit SQL Indexer
"""

import re
from pathlib import Path
from typing import List
from .models import TestKitMethod, TestKitComponent

class CSharpParser:
    """Parser for C# files"""
    
    def __init__(self):
        self.method_pattern = r'public\s+(static\s+)?(\w+(?:<[^>]+>)?)\s+(\w+)\s*\([^)]*\)'
    
    def extract_class_name(self, content: str) -> str:
        """Извлекает имя класса"""
        match = re.search(r'public\s+(static\s+)?class\s+(\w+)', content)
        return match.group(2) if match else ""

    def extract_class_summary(self, content: str) -> str:
        """Извлекает summary комментарий класса"""
        # Ищем /// <summary> перед объявлением класса
        pattern = r'/// <summary>\s*\n(.*?)\n.*?/// </summary>\s*\n.*?public\s+(static\s+)?class'
        match = re.search(pattern, content, re.DOTALL)
        if match:
            return re.sub(r'///\s*', '', match.group(1)).strip().replace('\n', ' ')
        return ""

    def extract_methods(self, content: str) -> List[TestKitMethod]:
        """Извлекает все методы из файла"""
        methods = []
        lines = content.split('\n')
        
        for i, line in enumerate(lines):
            match = re.search(self.method_pattern, line)
            if match:
                return_type = match.group(2)
                method_name = match.group(3)
                
                # Извлекаем параметры
                params_match = re.search(r'\(([^)]*)\)', line)
                parameters = []
                if params_match:
                    params_str = params_match.group(1)
                    if params_str.strip():
                        parameters = [p.strip() for p in params_str.split(',')]

                # Определяем статичность и generic
                is_static = 'static' in line
                is_generic = '<' in return_type or '<' in method_name
                
                # Извлекаем описание
                description = self.extract_method_summary(lines, i)
                
                # Определяем теги
                tags = self.determine_tags(method_name, return_type, description)
                
                # Создаем сигнатуру
                signature = line.strip()
                
                method = TestKitMethod(
                    name=method_name,
                    return_type=return_type,
                    parameters=parameters,
                    description=description,
                    tags=tags,
                    is_static=is_static,
                    is_generic=is_generic,
                    signature=signature,
                    line_number=i + 1
                )
                methods.append(method)
        
        return methods

    def extract_method_summary(self, lines: List[str], method_line: int) -> str:
        """Извлекает описание метода из комментариев"""
        description = ""
        
        # Ищем комментарии перед методом
        for i in range(method_line - 1, max(0, method_line - 10), -1):
            line = lines[i].strip()
            if line.startswith("///"):
                if "<summary>" in line:
                    # Начало summary
                    summary_lines = []
                    for j in range(i, len(lines)):
                        summary_line = lines[j].strip()
                        if "</summary>" in summary_line:
                            break
                        if summary_line.startswith("///"):
                            summary_lines.append(summary_line.replace("///", "").strip())
                    description = " ".join(summary_lines)
                    break
            elif line.startswith("//") or line.startswith("/*"):
                # Обычный комментарий
                comment = line.replace("//", "").replace("/*", "").replace("*/", "").strip()
                if comment:
                    description = comment
                    break
            elif line.strip() == "":
                continue
            else:
                break
        
        return description

    def determine_tags(self, method_name: str, return_type: str, description: str = "") -> List[str]:
        """Определяет теги на основе имени метода, типа возврата и описания"""
        tags = []
        
        # Анализируем имя метода
        name_lower = method_name.lower()
        
        # Базовые теги по имени
        if 'create' in name_lower:
            tags.append('factory')
        if 'build' in name_lower:
            tags.append('builder')
        if 'mock' in name_lower:
            tags.append('mock')
        if 'test' in name_lower:
            tags.append('test-infrastructure')
        
        # Теги по типу возврата
        return_lower = return_type.lower()
        if 'message' in return_lower:
            tags.append('message')
        if 'user' in return_lower:
            tags.append('user')
        if 'chat' in return_lower:
            tags.append('chat')
        if 'ban' in return_lower:
            tags.append('ban')
        if 'captcha' in return_lower:
            tags.append('captcha')
        if 'moderation' in return_lower:
            tags.append('moderation')
        if 'ai' in return_lower:
            tags.append('ai')
        if 'spam' in return_lower:
            tags.append('spam')
        if 'telegram' in return_lower:
            tags.append('telegram')
        if 'collection' in return_lower or 'list' in return_lower:
            tags.append('collection')
        
        # Анализируем описание
        desc_lower = description.lower()
        if 'valid' in desc_lower:
            tags.append('valid')
        if 'invalid' in desc_lower:
            tags.append('invalid')
        if 'realistic' in desc_lower:
            tags.append('realistic')
        if 'fake' in desc_lower:
            tags.append('fake')
        if 'bogus' in desc_lower:
            tags.append('bogus')
        if 'autofixture' in desc_lower:
            tags.append('autofixture')
        
        return list(set(tags))  # Убираем дубликаты

    def determine_category(self, relative_path: str) -> str:
        """Определяет категорию файла по пути"""
        path_lower = relative_path.lower()
        
        if 'golden' in path_lower:
            return 'golden-master'
        elif 'mock' in path_lower:
            return 'mocks'
        elif 'specialized' in path_lower:
            return 'specialized'
        elif 'autofixture' in path_lower:
            return 'autofixture'
        elif 'bogus' in path_lower:
            return 'bogus'
        elif 'telegram' in path_lower:
            return 'telegram'
        elif 'builder' in path_lower:
            return 'builders'
        elif 'infra' in path_lower:
            return 'infrastructure'
        else:
            return 'core' 