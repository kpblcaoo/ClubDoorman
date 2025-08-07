#!/usr/bin/env python3
"""
Скрипт для детального анализа покрытия тестами по файлам
"""

import os
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from collections import defaultdict

def parse_coverage_xml(coverage_file):
    """Парсит XML файл покрытия и возвращает статистику по файлам"""
    try:
        tree = ET.parse(coverage_file)
        root = tree.getroot()
        
        coverage_data = {}
        
        # Ищем все файлы в coverage
        for package in root.findall('.//package'):
            for file_elem in package.findall('.//file'):
                file_path = file_elem.get('name', '')
                if file_path:
                    # Извлекаем только имя файла
                    file_name = os.path.basename(file_path)
                    
                    # Считаем покрытые и непокрытые строки
                    covered_lines = 0
                    total_lines = 0
                    
                    for line in file_elem.findall('.//line'):
                        hits = int(line.get('hits', 0))
                        if hits > 0:
                            covered_lines += 1
                        total_lines += 1
                    
                    if total_lines > 0:
                        coverage_percent = (covered_lines / total_lines) * 100
                        coverage_data[file_name] = {
                            'covered': covered_lines,
                            'total': total_lines,
                            'percentage': coverage_percent,
                            'full_path': file_path
                        }
        
        return coverage_data
    except Exception as e:
        print(f"❌ Ошибка парсинга XML: {e}")
        return {}

def categorize_files_by_coverage(coverage_data):
    """Категоризирует файлы по уровню покрытия"""
    categories = {
        'high': [],      # >80%
        'medium': [],    # 50-80%
        'low': [],       # <50%
        'unknown': []    # нет данных
    }
    
    for file_name, data in coverage_data.items():
        percentage = data['percentage']
        
        if percentage >= 80:
            categories['high'].append((file_name, data))
        elif percentage >= 50:
            categories['medium'].append((file_name, data))
        elif percentage > 0:
            categories['low'].append((file_name, data))
        else:
            categories['unknown'].append((file_name, data))
    
    return categories

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

def generate_report(coverage_data, remaining_services, dependencies):
    """Генерирует отчет"""
    print("📊 ДЕТАЛЬНЫЙ АНАЛИЗ ПОКРЫТИЯ ТЕСТАМИ")
    print("=" * 60)
    
    # Категоризируем файлы
    categories = categorize_files_by_coverage(coverage_data)
    
    print(f"\n📈 СТАТИСТИКА ПОКРЫТИЯ:")
    print(f"Высокое покрытие (>80%): {len(categories['high'])} файлов")
    print(f"Среднее покрытие (50-80%): {len(categories['medium'])} файлов")
    print(f"Низкое покрытие (<50%): {len(categories['low'])} файлов")
    print(f"Без данных: {len(categories['unknown'])} файлов")
    
    print(f"\n🎯 КАНДИДАТЫ ДЛЯ ФАЗЫ 3:")
    print("=" * 40)
    
    # Показываем файлы с высоким покрытием (легкие кандидаты)
    if categories['high']:
        print(f"\n🟢 ЛЕГКИЕ КАНДИДАТЫ (Подфаза 3.1):")
        for file_name, data in sorted(categories['high'], key=lambda x: x[1]['percentage'], reverse=True):
            print(f"  ✅ {file_name}: {data['percentage']:.1f}% ({data['covered']}/{data['total']} строк)")
    
    # Показываем файлы со средним покрытием
    if categories['medium']:
        print(f"\n🟡 СРЕДНИЕ КАНДИДАТЫ (Подфаза 3.2):")
        for file_name, data in sorted(categories['medium'], key=lambda x: x[1]['percentage'], reverse=True):
            print(f"  ⚠️ {file_name}: {data['percentage']:.1f}% ({data['covered']}/{data['total']} строк)")
    
    # Показываем файлы с низким покрытием
    if categories['low']:
        print(f"\n🔴 СЛОЖНЫЕ КАНДИДАТЫ (Подфаза 3.3):")
        for file_name, data in sorted(categories['low'], key=lambda x: x[1]['percentage'], reverse=True):
            print(f"  ❌ {file_name}: {data['percentage']:.1f}% ({data['covered']}/{data['total']} строк)")
    
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
    
    if categories['high']:
        print("1. Начните с Подфазы 3.1 - файлы с высоким покрытием")
        print("   - Простые в тестировании")
        print("   - Минимальный риск регрессий")
    
    if categories['medium']:
        print("2. Подфаза 3.2 - файлы со средним покрытием")
        print("   - Требуют дополнительных тестов")
        print("   - Средний риск")
    
    if categories['low']:
        print("3. Подфаза 3.3 - файлы с низким покрытием")
        print("   - Критически важны дополнительные тесты")
        print("   - Высокий риск регрессий")

def main():
    print("🔍 Анализ покрытия тестами для подготовки к Фазе 3...")
    
    # Ищем файл покрытия
    coverage_files = list(Path("TestResults").rglob("*.xml"))
    
    if not coverage_files:
        print("❌ Файл покрытия не найден. Запустите тесты с покрытием:")
        print("   dotnet test --collect:\"XPlat Code Coverage\"")
        return
    
    coverage_file = coverage_files[0]
    print(f"📄 Найден файл покрытия: {coverage_file}")
    
    # Парсим покрытие
    coverage_data = parse_coverage_xml(coverage_file)
    
    if not coverage_data:
        print("❌ Не удалось извлечь данные покрытия")
        return
    
    # Анализируем оставшиеся сервисы
    remaining_services = find_remaining_services()
    
    # Анализируем зависимости
    dependencies = analyze_dependencies()
    
    # Генерируем отчет
    generate_report(coverage_data, remaining_services, dependencies)
    
    # Сохраняем детальные данные
    with open("coverage_analysis_report.txt", "w", encoding="utf-8") as f:
        f.write("ДЕТАЛЬНЫЙ АНАЛИЗ ПОКРЫТИЯ\n")
        f.write("=" * 50 + "\n\n")
        
        for file_name, data in sorted(coverage_data.items(), key=lambda x: x[1]['percentage'], reverse=True):
            f.write(f"{file_name}: {data['percentage']:.1f}% ({data['covered']}/{data['total']})\n")
    
    print(f"\n📄 Детальный отчет сохранен в: coverage_analysis_report.txt")

if __name__ == "__main__":
    main() 