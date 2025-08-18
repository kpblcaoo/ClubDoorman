# ClubDoorman.Baseline

Изолированный консольный harness для генерации Golden Master baseline снапшотов.

Цели:
* Никаких baseline/тест костылей в production `Program.cs`.
* Детерминированные ID и стабильная дата-папка (`golden/baseline`).
* Синтетический Telegram wrapper + сидер двух сообщений.

Запуск:
```
./scripts/generate_golden_baseline.sh --clean
```

Результат: четыре файла в `golden/baseline`:
* `<hash>.input.json` (вход апдейта)
* `<hash>.output.json` (решение модерации)

Первое сообщение помечается как `Delete` (банальное приветствие), второе `Allow`.

Расширение:
1. Добавьте новые synthetic updates в `GoldenBaselineSeeder` (сохраняйте стабильный порядок).
2. Перегенерируйте baseline скриптом.
3. Используйте golden diff инструмент для регрессионного сравнения.

Важно: Не менять prod код только ради baseline – всё, что специфично baseline, остаётся здесь.
