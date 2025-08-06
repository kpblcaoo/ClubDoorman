#!/usr/bin/env python3
"""
Анализатор зависимостей DI для ClubDoorman
Анализирует Program.cs и выявляет группы сервисов для рефакторинга
"""

import re
import os
from collections import defaultdict, deque
from typing import Dict, List, Set, Tuple

class DIDependencyAnalyzer:
    def __init__(self, project_root: str):
        self.project_root = project_root
        self.services = {}
        self.dependencies = defaultdict(set)
        self.reverse_dependencies = defaultdict(set)
        
    def analyze_program_cs(self):
        """Анализирует Program.cs и извлекает зависимости"""
        program_cs_path = os.path.join(self.project_root, "ClubDoorman", "Program.cs")
        
        if not os.path.exists(program_cs_path):
            print(f"❌ Файл {program_cs_path} не найден")
            return
            
        with open(program_cs_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # Извлекаем все регистрации сервисов
        service_pattern = r'services\.AddSingleton<([^>]+)>\(provider\s*=>\s*\{[^}]*new\s+([^(]+)\(([^)]*)\)'
        matches = re.finditer(service_pattern, content, re.DOTALL)
        
        for match in matches:
            interface_name = match.group(1).strip()
            class_name = match.group(2).strip()
            constructor_params = match.group(3).strip()
            
            # Извлекаем зависимости из параметров конструктора
            deps = self._extract_dependencies(constructor_params)
            
            self.services[interface_name] = class_name
            self.dependencies[interface_name] = deps
            
            # Строим обратные зависимости
            for dep in deps:
                self.reverse_dependencies[dep].add(interface_name)
                
    def _extract_dependencies(self, constructor_params: str) -> Set[str]:
        """Извлекает зависимости из параметров конструктора"""
        deps = set()
        
        # Паттерн для извлечения зависимостей
        dep_pattern = r'provider\.GetRequiredService<([^>]+)>'
        matches = re.findall(dep_pattern, constructor_params)
        
        for match in matches:
            deps.add(match.strip())
            
        return deps
        
    def find_cycles(self) -> List[List[str]]:
        """Находит циклические зависимости"""
        cycles = []
        visited = set()
        rec_stack = set()
        
        def dfs(node: str, path: List[str]):
            if node in rec_stack:
                cycle_start = path.index(node)
                cycles.append(path[cycle_start:] + [node])
                return
                
            if node in visited:
                return
                
            visited.add(node)
            rec_stack.add(node)
            path.append(node)
            
            for dep in self.dependencies.get(node, set()):
                dfs(dep, path.copy())
                
            rec_stack.remove(node)
            
        for service in self.services:
            if service not in visited:
                dfs(service, [])
                
        return cycles
        
    def group_services(self) -> Dict[str, List[str]]:
        """Группирует сервисы по функциональности"""
        groups = {
            'Core': [],
            'Telegram': [],
            'Moderation': [],
            'AI': [],
            'UserManagement': [],
            'Messaging': [],
            'Statistics': [],
            'Configuration': [],
            'Logging': [],
            'Commands': [],
            'Handlers': []
        }
        
        # Маппинг сервисов по группам
        service_groups = {
            # Core
            'IAppConfig': 'Configuration',
            'AppConfig': 'Configuration',
            
            # Telegram
            'TelegramBotClient': 'Telegram',
            'ITelegramBotClient': 'Telegram',
            'ITelegramBotClientWrapper': 'Telegram',
            'TelegramBotClientWrapper': 'Telegram',
            
            # Moderation
            'IModerationService': 'Moderation',
            'ModerationService': 'Moderation',
            'ISpamHamClassifier': 'Moderation',
            'SpamHamClassifier': 'Moderation',
            'IMimicryClassifier': 'Moderation',
            'MimicryClassifier': 'Moderation',
            'IBadMessageManager': 'Moderation',
            'BadMessageManager': 'Moderation',
            'ISuspiciousUsersStorage': 'Moderation',
            'SuspiciousUsersStorage': 'Moderation',
            'IViolationTracker': 'Moderation',
            'ViolationTracker': 'Moderation',
            'IUserBanService': 'Moderation',
            'UserBanService': 'Moderation',
            'IChannelModerationService': 'Moderation',
            'ChannelModerationService': 'Moderation',
            
            # AI
            'IAiChecks': 'AI',
            'AiChecks': 'AI',
            
            # User Management
            'IUserManager': 'UserManagement',
            'UserManager': 'UserManagement',
            'ApprovedUsersStorage': 'UserManagement',
            'IUserCleanupService': 'UserManagement',
            'UserCleanupService': 'UserManagement',
            'IUserJoinService': 'UserManagement',
            'UserJoinService': 'UserManagement',
            
            # Messaging
            'IMessageService': 'Messaging',
            'MessageService': 'Messaging',
            'MessageTemplates': 'Messaging',
            'IServiceChatDispatcher': 'Messaging',
            'ServiceChatDispatcher': 'Messaging',
            'ILogChatService': 'Messaging',
            'LogChatService': 'Messaging',
            'INotificationService': 'Messaging',
            'NotificationService': 'Messaging',
            'IChatLinkFormatter': 'Messaging',
            'ChatLinkFormatter': 'Messaging',
            
            # Statistics
            'IStatisticsService': 'Statistics',
            'StatisticsService': 'Statistics',
            'GlobalStatsManager': 'Statistics',
            
            # Captcha
            'ICaptchaService': 'Core',
            'CaptchaService': 'Core',
            
            # Logging
            'ILoggingConfigurationService': 'Logging',
            'LoggingConfigurationService': 'Logging',
            'IUserFlowLogger': 'Logging',
            'UserFlowLogger': 'Logging',
            
            # Commands
            'ICommandProcessingService': 'Commands',
            'CommandProcessingService': 'Commands',
            'ICommandHandler': 'Commands',
            'StartCommandHandler': 'Commands',
            'SuspiciousCommandHandler': 'Commands',
            
            # Handlers
            'IUpdateHandler': 'Handlers',
            'MessageHandler': 'Handlers',
            'CallbackQueryHandler': 'Handlers',
            'ChatMemberHandler': 'Handlers',
            'IMessageHandler': 'Handlers',
            
            # Dispatcher
            'IUpdateDispatcher': 'Core',
            'UpdateDispatcher': 'Core',
            
            # Permissions
            'IBotPermissionsService': 'Core',
            'BotPermissionsService': 'Core',
            
            # Intro Flow
            'IntroFlowService': 'Core',
            
            # Text Processing
            'TextProcessor': 'Core',
            'SimpleFilters': 'Core',
        }
        
        for service in self.services:
            group = service_groups.get(service, 'Core')
            if service not in groups[group]:
                groups[group].append(service)
                
        return groups
        
    def analyze_dependency_complexity(self) -> Dict[str, int]:
        """Анализирует сложность зависимостей каждого сервиса"""
        complexity = {}
        
        for service, deps in self.dependencies.items():
            complexity[service] = len(deps)
            
        return complexity
        
    def find_high_dependency_services(self, threshold: int = 5) -> List[Tuple[str, int]]:
        """Находит сервисы с большим количеством зависимостей"""
        complexity = self.analyze_dependency_complexity()
        high_deps = [(service, count) for service, count in complexity.items() if count >= threshold]
        return sorted(high_deps, key=lambda x: x[1], reverse=True)
        
    def suggest_refactoring(self) -> Dict[str, List[str]]:
        """Предлагает рефакторинг на основе анализа"""
        groups = self.group_services()
        cycles = self.find_cycles()
        high_deps = self.find_high_dependency_services(8)
        
        suggestions = {
            'extract_modules': [],
            'reduce_dependencies': [],
            'fix_cycles': [],
            'group_suggestions': {}
        }
        
        # Предложения по извлечению модулей
        if groups['Moderation']:
            suggestions['extract_modules'].append({
                'name': 'ModerationModule',
                'services': groups['Moderation'],
                'reason': 'Большое количество связанных сервисов модерации'
            })
            
        if groups['Messaging']:
            suggestions['extract_modules'].append({
                'name': 'MessagingModule', 
                'services': groups['Messaging'],
                'reason': 'Централизованная система сообщений'
            })
            
        if groups['UserManagement']:
            suggestions['extract_modules'].append({
                'name': 'UserManagementModule',
                'services': groups['UserManagement'], 
                'reason': 'Управление пользователями и их состоянием'
            })
            
        # Сервисы с большим количеством зависимостей
        for service, count in high_deps:
            suggestions['reduce_dependencies'].append({
                'service': service,
                'dependency_count': count,
                'suggestion': 'Рассмотреть выделение в отдельный модуль или упрощение'
            })
            
        # Циклические зависимости
        for cycle in cycles:
            suggestions['fix_cycles'].append({
                'cycle': cycle,
                'suggestion': 'Использовать события или промежуточные интерфейсы'
            })
            
        return suggestions

def main():
    analyzer = DIDependencyAnalyzer('.')
    analyzer.analyze_program_cs()
    
    print("🔍 Анализ зависимостей DI ClubDoorman")
    print("=" * 50)
    
    # Группировка сервисов
    groups = analyzer.group_services()
    print("\n📦 Группировка сервисов:")
    for group_name, services in groups.items():
        if services:
            print(f"\n{group_name}:")
            for service in services:
                print(f"  - {service}")
                
    # Сложность зависимостей
    complexity = analyzer.analyze_dependency_complexity()
    print(f"\n📊 Сложность зависимостей:")
    for service, count in sorted(complexity.items(), key=lambda x: x[1], reverse=True)[:10]:
        print(f"  {service}: {count} зависимостей")
        
    # Циклические зависимости
    cycles = analyzer.find_cycles()
    if cycles:
        print(f"\n⚠️  Найдены циклические зависимости:")
        for cycle in cycles:
            print(f"  {' -> '.join(cycle)}")
    else:
        print(f"\n✅ Циклических зависимостей не найдено")
        
    # Предложения по рефакторингу
    suggestions = analyzer.suggest_refactoring()
    print(f"\n💡 Предложения по рефакторингу:")
    
    print(f"\n🔧 Извлечение модулей:")
    for module in suggestions['extract_modules']:
        print(f"  {module['name']}: {module['reason']}")
        for service in module['services']:
            print(f"    - {service}")
            
    print(f"\n⚠️  Сервисы с большим количеством зависимостей:")
    for item in suggestions['reduce_dependencies']:
        print(f"  {item['service']}: {item['dependency_count']} зависимостей")
        print(f"    {item['suggestion']}")

if __name__ == "__main__":
    main() 