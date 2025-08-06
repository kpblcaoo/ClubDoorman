#!/usr/bin/env python3
"""
Скрипт для анализа оставшихся сервисов для Фазы 3
"""

import os
import re
from pathlib import Path
from collections import defaultdict

def find_remaining_services():
    """Находит сервисы, которые еще не вынесены в модули"""
    services_dir = Path("ClubDoorman/Services")
    remaining_services = []
    
    # Файлы, которые уже в модулях
    modularized_files = {
        'ConfigurationModule.cs', 'TelegramModule.cs', 'StatisticsModule.cs', 'AIModule.cs',
        'UserManagementModule.cs', 'MessagingModule.cs', 'CaptchaModule.cs', 'CommandsModule.cs',
        'HandlersModule.cs'
    }
    
    # Ищем .cs файлы в Services
    for cs_file in services_dir.rglob("*.cs"):
        if cs_file.name not in modularized_files and not cs_file.name.endswith('.bak'):
            relative_path = cs_file.relative_to(services_dir)
            remaining_services.append(str(relative_path))
    
    return remaining_services

def analyze_dependencies():
    """Анализирует зависимости между сервисами"""
    dependencies = defaultdict(set)
    
    # Простой анализ using директив
    services_dir = Path("ClubDoorman/Services")
    
    for cs_file in services_dir.rglob("*.cs"):
        try:
            with open(cs_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Ищем using директивы
            using_pattern = r'using\s+ClubDoorman\.Services\.([^;]+);'
            matches = re.findall(using_pattern, content)
            
            current_service = cs_file.parent.name
            for match in matches:
                dependencies[current_service].add(match)
                
        except Exception as e:
            print(f"⚠️ Ошибка анализа {cs_file}: {e}")
    
    return dependencies

def analyze_service_complexity():
    """Анализирует сложность сервисов по количеству строк и зависимостей"""
    services_dir = Path("ClubDoorman/Services")
    service_stats = {}
    
    for cs_file in services_dir.rglob("*.cs"):
        if cs_file.name.endswith('.bak'):
            continue
            
        try:
            with open(cs_file, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            # Считаем строки кода (исключаем пустые и комментарии)
            code_lines = 0
            for line in lines:
                stripped = line.strip()
                if stripped and not stripped.startswith('//') and not stripped.startswith('/*'):
                    code_lines += 1
            
            service_stats[cs_file.name] = {
                'total_lines': len(lines),
                'code_lines': code_lines,
                'path': str(cs_file.relative_to(services_dir))
            }
                
        except Exception as e:
            print(f"⚠️ Ошибка анализа {cs_file}: {e}")
    
    return service_stats

def categorize_services_by_complexity(service_stats):
    """Категоризирует сервисы по сложности"""
    categories = {
        'simple': [],      # <100 строк кода
        'medium': [],      # 100-300 строк кода
        'complex': [],     # >300 строк кода
    }
    
    for service_name, stats in service_stats.items():
        code_lines = stats['code_lines']
        
        if code_lines < 100:
            categories['simple'].append((service_name, stats))
        elif code_lines < 300:
            categories['medium'].append((service_name, stats))
        else:
            categories['complex'].append((service_name, stats))
    
    return categories

def generate_phase3_plan(remaining_services, dependencies, service_stats):
    """Генерирует план для Фазы 3"""
    print("🚀 ПЛАН ПОДГОТОВКИ К ФАЗЕ 3")
    print("=" * 50)
    
    # Категоризируем по сложности
    categories = categorize_services_by_complexity(service_stats)
    
    print(f"\n📊 АНАЛИЗ ОСТАВШИХСЯ СЕРВИСОВ:")
    print(f"Простые (<100 строк): {len(categories['simple'])}")
    print(f"Средние (100-300 строк): {len(categories['medium'])}")
    print(f"Сложные (>300 строк): {len(categories['complex'])}")
    
    print(f"\n🎯 КАНДИДАТЫ ДЛЯ ПОДФАЗ:")
    print("=" * 40)
    
    # Подфаза 3.1: Простые сервисы
    if categories['simple']:
        print(f"\n🟢 ПОДФАЗА 3.1 - ПРОСТЫЕ СЕРВИСЫ:")
        for service_name, stats in sorted(categories['simple'], key=lambda x: x[1]['code_lines']):
            print(f"  ✅ {service_name}: {stats['code_lines']} строк кода")
    
    # Подфаза 3.2: Средние сервисы
    if categories['medium']:
        print(f"\n🟡 ПОДФАЗА 3.2 - СРЕДНИЕ СЕРВИСЫ:")
        for service_name, stats in sorted(categories['medium'], key=lambda x: x[1]['code_lines']):
            print(f"  ⚠️ {service_name}: {stats['code_lines']} строк кода")
    
    # Подфаза 3.3: Сложные сервисы
    if categories['complex']:
        print(f"\n🔴 ПОДФАЗА 3.3 - СЛОЖНЫЕ СЕРВИСЫ:")
        for service_name, stats in sorted(categories['complex'], key=lambda x: x[1]['code_lines']):
            print(f"  ❌ {service_name}: {stats['code_lines']} строк кода")
    
    print(f"\n📁 ОСТАВШИЕСЯ СЕРВИСЫ:")
    print("=" * 30)
    for service in remaining_services:
        print(f"  📄 {service}")
    
    print(f"\n🔗 АНАЛИЗ ЗАВИСИМОСТЕЙ:")
    print("=" * 30)
    for service, deps in dependencies.items():
        if deps:
            print(f"  {service} -> {', '.join(deps)}")
    
    # Рекомендации
    print(f"\n💡 РЕКОМЕНДАЦИИ:")
    print("=" * 20)
    
    if categories['simple']:
        print("1. Начните с Подфазы 3.1 - простые сервисы")
        print("   - Минимальный риск")
        print("   - Быстрое выполнение")
    
    if categories['medium']:
        print("2. Подфаза 3.2 - средние сервисы")
        print("   - Требуют внимания к зависимостям")
        print("   - Средний риск")
    
    if categories['complex']:
        print("3. Подфаза 3.3 - сложные сервисы")
        print("   - Критически важны дополнительные тесты")
        print("   - Высокий риск регрессий")
        print("   - Возможно разбиение на подмодули")

def main():
    print("🔍 Анализ оставшихся сервисов для Фазы 3...")
    
    # Находим оставшиеся сервисы
    remaining_services = find_remaining_services()
    
    # Анализируем зависимости
    dependencies = analyze_dependencies()
    
    # Анализируем сложность сервисов
    service_stats = analyze_service_complexity()
    
    # Генерируем план
    generate_phase3_plan(remaining_services, dependencies, service_stats)
    
    # Сохраняем детальные данные
    with open("remaining_services_analysis.txt", "w", encoding="utf-8") as f:
        f.write("АНАЛИЗ ОСТАВШИХСЯ СЕРВИСОВ\n")
        f.write("=" * 50 + "\n\n")
        
        f.write("ОСТАВШИЕСЯ СЕРВИСЫ:\n")
        for service in remaining_services:
            f.write(f"- {service}\n")
        
        f.write("\nСТАТИСТИКА СЛОЖНОСТИ:\n")
        for service_name, stats in sorted(service_stats.items(), key=lambda x: x[1]['code_lines'], reverse=True):
            f.write(f"- {service_name}: {stats['code_lines']} строк кода\n")
        
        f.write("\nЗАВИСИМОСТИ:\n")
        for service, deps in dependencies.items():
            if deps:
                f.write(f"- {service} -> {', '.join(deps)}\n")
    
    print(f"\n📄 Детальный анализ сохранен в: remaining_services_analysis.txt")

if __name__ == "__main__":
    main() 