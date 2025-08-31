# План этапа 04: UserIndex (индекс username → userId)

## Цель
- Ввести отдельный индекс username → userId с TTL
- Убрать O(n) обходы MemoryCache

## Шаги
1. Реализовать сервис IUserIndex/UserIndex
2. Внедрить UserIndex в нужные места (MessageHandler, UserJoin и т.д.)
3. Заменить обходы MemoryCache на UserIndex
4. Протестировать работу индекса
5. Описать изменения в ворклоге

## Ветка
- `refactor/cache-index`

## Критерии готовности
- Нет обходов MemoryCache.Default для поиска userId по username
- Все тесты проходят
