#!/usr/bin/env python3
"""
Скрипт для автоматического создания модулей DI рефакторинга
Использование: python create_module.py <service_name> <module_name>
Пример: python create_module.py ChatLinkFormatter LinkFormatting
"""

import os
import sys
import re
import subprocess
from pathlib import Path

def find_service_file(service_name):
    """Находит файл сервиса в проекте"""
    project_root = Path(__file__).parent.parent
    service_file = None
    
    # Ищем в Services/
    for root, dirs, files in os.walk(project_root / "ClubDoorman" / "Services"):
        for file in files:
            if file == f"{service_name}.cs":
                service_file = Path(root) / file
                break
        if service_file:
            break
    
    return service_file

def get_current_namespace(service_file):
    """Извлекает текущий namespace из файла"""
    with open(service_file, 'r', encoding='utf-8') as f:
        content = f.read()
        match = re.search(r'namespace\s+([^;\s]+)', content)
        return match.group(1) if match else None

def create_module_directory(module_name):
    """Создает директорию для модуля"""
    project_root = Path(__file__).parent.parent
    module_dir = project_root / "ClubDoorman" / "Services" / module_name
    module_dir.mkdir(exist_ok=True)
    return module_dir

def move_service_file(service_file, module_dir):
    """Перемещает файл сервиса в директорию модуля"""
    new_path = module_dir / service_file.name
    service_file.rename(new_path)
    return new_path

def update_namespace(service_file, new_namespace):
    """Обновляет namespace в файле сервиса"""
    with open(service_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Заменяем namespace
    content = re.sub(
        r'namespace\s+[^;\s]+',
        f'namespace {new_namespace}',
        content
    )
    
    # Удаляем неиспользуемые using директивы
    lines = content.split('\n')
    filtered_lines = []
    for line in lines:
        if line.strip().startswith('using ClubDoorman.Services.'):
            # Проверяем, используется ли этот using
            if not re.search(r'\b' + line.split('.')[-1].rstrip(';') + r'\b', content):
                continue
        filtered_lines.append(line)
    
    content = '\n'.join(filtered_lines)
    
    with open(service_file, 'w', encoding='utf-8') as f:
        f.write(content)

def create_module_file(module_dir, module_name):
    """Создает файл модуля"""
    module_file = module_dir / f"{module_name}Module.cs"
    
    template = f'''using Microsoft.Extensions.DependencyInjection;

namespace ClubDoorman.Services.{module_name};

public static class {module_name}Module
{{
    public static IServiceCollection Add{module_name}Services(this IServiceCollection services)
    {{
        // TODO: Добавить регистрацию сервисов если необходимо
        return services;
    }}
}}
'''
    
    with open(module_file, 'w', encoding='utf-8') as f:
        f.write(template)

def update_program_cs(module_name):
    """Обновляет Program.cs для добавления модуля"""
    program_file = Path(__file__).parent.parent / "ClubDoorman" / "Program.cs"
    
    with open(program_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Добавляем using директиву
    using_pattern = r'(using ClubDoorman\.Services\.[^;]+;)'
    new_using = f'using ClubDoorman.Services.{module_name};'
    
    if new_using not in content:
        # Находим последний using и добавляем после него
        lines = content.split('\n')
        for i, line in enumerate(lines):
            if line.strip().startswith('using ClubDoorman.Services.') and i + 1 < len(lines):
                if not lines[i + 1].strip().startswith('using ClubDoorman.Services.'):
                    lines.insert(i + 1, new_using)
                    break
        content = '\n'.join(lines)
    
    # Добавляем вызов модуля
    add_pattern = r'(services\.Add\w+Services\(\);)\s*'
    new_add = f'services.Add{module_name}Services();'
    
    if new_add not in content:
        # Находим последний Add*Services() и добавляем после него
        lines = content.split('\n')
        for i, line in enumerate(lines):
            if 'Add' in line and 'Services()' in line and i + 1 < len(lines):
                if not lines[i + 1].strip().startswith('services.Add'):
                    lines.insert(i + 1, '                ' + new_add)
                    break
        content = '\n'.join(lines)
    
    with open(program_file, 'w', encoding='utf-8') as f:
        f.write(content)

def find_files_using_service(service_name):
    """Находит все файлы, которые используют сервис"""
    project_root = Path(__file__).parent.parent
    files_to_update = []
    
    for root, dirs, files in os.walk(project_root):
        for file in files:
            if file.endswith('.cs'):
                file_path = Path(root) / file
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        if f'{service_name}.' in content:
                            files_to_update.append(file_path)
                except:
                    continue
    
    return files_to_update

def update_using_directives(files_to_update, new_namespace):
    """Обновляет using директивы в файлах"""
    for file_path in files_to_update:
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Добавляем using если его нет
            if new_namespace not in content:
                lines = content.split('\n')
                for i, line in enumerate(lines):
                    if line.strip().startswith('using ClubDoorman.Services.') and i + 1 < len(lines):
                        if not lines[i + 1].strip().startswith('using ClubDoorman.Services.'):
                            lines.insert(i + 1, f'using {new_namespace};')
                            break
                content = '\n'.join(lines)
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
        except:
            continue

def run_tests():
    """Запускает тесты для проверки"""
    print("🧪 Запускаем тесты...")
    result = subprocess.run(['dotnet', 'test', '--filter', 'Category!=Demo'], 
                          capture_output=True, text=True)
    return result.returncode == 0

def main():
    if len(sys.argv) != 3:
        print("Использование: python create_module.py <service_name> <module_name>")
        print("Пример: python create_module.py ChatLinkFormatter LinkFormatting")
        sys.exit(1)
    
    service_name = sys.argv[1]
    module_name = sys.argv[2]
    
    print(f"🚀 Создаем модуль {module_name} для сервиса {service_name}")
    
    # 1. Находим файл сервиса
    service_file = find_service_file(service_name)
    if not service_file:
        print(f"❌ Файл {service_name}.cs не найден")
        sys.exit(1)
    
    print(f"📁 Найден файл: {service_file}")
    
    # 2. Получаем текущий namespace
    current_namespace = get_current_namespace(service_file)
    new_namespace = f"ClubDoorman.Services.{module_name}"
    
    print(f"🔄 Обновляем namespace: {current_namespace} -> {new_namespace}")
    
    # 3. Создаем директорию модуля
    module_dir = create_module_directory(module_name)
    print(f"📂 Создана директория: {module_dir}")
    
    # 4. Перемещаем файл сервиса
    new_service_file = move_service_file(service_file, module_dir)
    print(f"📦 Перемещен файл: {new_service_file}")
    
    # 5. Обновляем namespace в файле сервиса
    update_namespace(new_service_file, new_namespace)
    print(f"✏️ Обновлен namespace в {new_service_file.name}")
    
    # 6. Создаем файл модуля
    create_module_file(module_dir, module_name)
    print(f"📄 Создан файл модуля: {module_name}Module.cs")
    
    # 7. Обновляем Program.cs
    update_program_cs(module_name)
    print(f"🔧 Обновлен Program.cs")
    
    # 8. Находим и обновляем файлы, использующие сервис
    files_to_update = find_files_using_service(service_name)
    update_using_directives(files_to_update, new_namespace)
    print(f"📝 Обновлены using директивы в {len(files_to_update)} файлах")
    
    # 9. Запускаем тесты
    if run_tests():
        print("✅ Тесты прошли успешно!")
    else:
        print("⚠️ Тесты не прошли, проверьте изменения вручную")
    
    print(f"\n🎉 Модуль {module_name} создан успешно!")
    print(f"📁 Расположение: {module_dir}")
    print(f"📄 Файлы: {new_service_file.name}, {module_name}Module.cs")

if __name__ == "__main__":
    main() 