# Golden Master / Baseline Usage Guide

(Рус / Eng mixed for clarity)

## 1. Назначение
Golden Master слой фиксирует ожидаемое детерминированное поведение пайплайна модерации. Он используется, чтобы:
- Выявлять непреднамеренные изменения логики (drift)
- Гарантировать целостность и согласованность разных «слоёв» представления события
- Обеспечить воспроизводимость refactor'ов

## 2. Слои и их файлы
| Layer | Schema | Расположение | Назначение |
|-------|--------|--------------|-----------|
| Manifest | 1 | `golden/manifest.json` | Список Entries (Id, CorrelationId, ShortName, ExpectedAction, RuleCode) |
| V2 Snapshots | 2 | `golden/baseline_v2/*.v2.json` | Минимальные снапшоты исходного сообщения после парсинга |
| Normalized | 4 | `golden/normalized/*.norm.json` | Нормализованное представление (canonical) |
| Aggregates | 5 | `golden/aggregates.json` | Сводные метрики/подсчёты (Total, ActionCounts, RuleCodeCounts) |
| (Removed) v1 Output | — | (удалено) | Исторический слой *.output.json — де-прекейтед (Phase 9) |
| (Transient) Semantics | — | `*.sem.json` (не в git) | Временные файлы с семантикой для билдеров (не коммитим) |

## 3. Транзиентные semantics (`*.sem.json`)
- Генерируются `GoldenMasterRecorder`
- Потребляются билдерами Manifest/V2/Norm/Aggregates
- Не входят в репозиторий (Hygiene тест гарантирует)

## 4. Инварианты (тесты Golden*)
1. Manifest:
   - Нет неожиданных дыр в Id (разрешён whitelist: 8, 9, 17)
   - У каждой Entry заданы `ExpectedAction` и `RuleCode` (RuleCode != `Unknown`)
2. V2 / Norm соответствуют Manifest по CorrelationId
3. Aggregates:
   - `Total == Entries.Count`
   - Подсчёты по Action / RuleCode совпадают c вычисленными
4. Hygiene:
   - Отсутствуют любые `*.output.json`
   - Отсутствуют закоммиченные `*.sem.json`

## 5. CI Поведение (`.github/workflows/golden-validation.yml`)
Steps:
1. Checkout + build
2. Регена baseline harness (`dotnet run --project ClubDoorman.Baseline ...`)
3. `git status --porcelain -- ClubDoorman.Baseline/golden`
   - Если есть изменения → Job FAIL (diff printed) → baseline НЕ перезаписывается автоматически
4. Запуск только Golden категорий тестов
5. Артефакты выгружаются (для инспекции) без изменения git

Таким образом baseline «заморожен» пока разработчик сам не закоммитит новые артефакты.

## 6. Как обновить baseline
```
# 1. Изменяешь код / добавляешь сценарий
# 2. Регенерация
DOTNET_ENVIRONMENT=Development \
  dotnet run --project ClubDoorman.Baseline/ClubDoorman.Baseline.csproj -c Release --no-launch-profile

# 3. Смотришь diff
git status -- ClubDoorman.Baseline/golden
git diff -- ClubDoorman.Baseline/golden

# 4. Локально прогоняешь golden тесты
dotnet test ClubDoorman.Test --filter "(Category=GoldenManifest|Category=GoldenV2|Category=GoldenNorm|Category=GoldenAgg|Category=GoldenHygiene)"

# 5. Убеждаешься что инварианты соблюдены
# 6. Commit + push
```
CI после push должен показать *No golden diffs detected.*

## 7. Добавление нового примера
1. Определи следующий свободный `Id` (не создавая лишних дыр; whitelist дыр уже занят 8,9,17 — не добавляй новые без причины)
2. Запусти регенерацию (см. §6)
3. Найди новую запись в `manifest.json` и проставь:
   - `ShortName` (лаконично)
   - `ExpectedAction` (Allow / Delete / Report / …)
   - `RuleCode` (конкретный код, не `Unknown`)
4. Убедись что появился соответствующий `.v2.json` и `.norm.json`
5. Запусти golden тесты
6. Commit

## 8. Типичные ошибки и решения
| Симптом | Причина | Решение |
|---------|---------|---------|
| Golden validation job падает с diff | Забыл закоммитить новые артефакты | Просмотреть diff → принять (commit) или откатить код |
| GoldenManifestTests: missing ExpectedAction | Новая/изменённая Entry без поля | Допиши поле и обнови aggregates (регенерация) |
| Aggregates test: Action count mismatch | `aggregates.json` устарел | Регенерация baseline и commit |
| Hygiene test fail (найден *.output.json) | Случайно восстановлен legacy файл | Удалить файл, commit |
| Hygiene test fail (найден *.sem.json) | Закоммитил транзиент | Удалить из git, добавить в .gitignore если нужно |

## 9. Быстрый скрипт (можно создать `scripts/update_golden.sh`)
Пример содержимого:
```
#!/usr/bin/env bash
set -euo pipefail
proj=ClubDoorman.Baseline/ClubDoorman.Baseline.csproj
echo "[Golden] Regenerating..."
dotnet run --project "$proj" -c Release --no-launch-profile

echo "[Golden] Diff:" 
if git diff --quiet -- ClubDoorman.Baseline/golden; then
  echo "  (no changes)"
else
  git --no-pager diff --name-status -- ClubDoorman.Baseline/golden
fi

echo "[Golden] Running tests..."
dotnet test ClubDoorman.Test --filter "(Category=GoldenManifest|Category=GoldenV2|Category=GoldenNorm|Category=GoldenAgg|Category=GoldenHygiene)" --verbosity minimal
```
(Скрипт не добавлен автоматически, чтобы не плодить churn — добавь при необходимости.)

## 10. FAQ
**Q:** Почему мы удалили *.output.json?
**A:** Устаревший формат v1; семантика перенесена в транзиентные *.sem.json; исключает двусмысленность и шум.

**Q:** Можно ли обновить baseline напрямую в CI?
**A:** Нет. Нужно локально сгенерировать и закоммитить.

**Q:** Что если нужно временно разрешить новую дыру в Id?
**A:** Добавь её в whitelist в тесте Manifest (осознанно) + документируй причину.

## 11. Термины
- Drift: непреднамеренное изменение артефактов
- Golden Harness: baseline генератор (Baseline проект)
- Semantics file: временный JSON c результатами анализа для билдеров

## 12. Semantics (*.sem.json) — глубже и roadmap
### Что внутри
`<correlationId>.sem.json` содержит минимальную «семантику» результата обработки: 
```
{ "action": "Allow|Delete|Report|…", "ruleCode": "StopWords|Emoji|Banlist|Command|…" }
```
Эти данные:
1. Пишет `GoldenMasterRecorder.TryRecordOutput` если из результата можно извлечь `action` / `ruleCode`.
2. Используются билдерами (`GoldenManifestBuilder`, `GoldenV2Exporter`, `GoldenNormalizationBuilder`) как источник правды для заполнения `ExpectedAction` и `RuleCode`, снижая дублирование логики.

### Почему не коммитим
Три причины:
1. Шум: при любых правках текста reason/структуры результирующие файлы будут меняться пачкой.
2. Детерминизм: CI регенерирует артефакты без сетевых/случайных дрожаний — меньше ложных diffs.
3. Текущий компромисс: часть сценариев пока не всегда эмитит семантику (см. overrides ниже) — не хотим фиксировать неполные файлы в истории.

### SemanticsOverrides (Удалены)
Ранее существовал временный словарь `SemanticsOverrides` (Id 12=/start, Id 15=BanlistUser) для заполнения `ExpectedAction`/`RuleCode`, пока recorder не покрывал эти ранние пути.

Сейчас overrides удалены (#137). Если для какого-либо сценария не появился `.sem.json` и поля в manifest стали null — это явный сигнал расширить запись семантики (GoldenMasterRecorder) или добавить тестовый путь.

### Workflow локальной регенерации (с учётом семантики)
1. Включить запись (env / флаги) и прогнать сценарий / тесты, чтобы появились `.sem.json` (они лягут рядом с `.input.json`).
2. Запустить baseline генератор — manifest подхватит свежие семантики.
3. Перед коммитом: удалить или просто оставить — hygiene тест упадёт ТОЛЬКО если файл попал в индекс git (tracked). Проще: не добавлять их (`git add -p` / pathspec).

### FAQ (semantics)
Q: Почему hygiene тест раньше падал у меня локально?  
A: Были закоммичены *.sem.json (или тест ещё не различал tracked/untracked); теперь он проверяет только tracked.

Q: Нужно ли руками редактировать sem.json?  
A: Нет — правь код, регенерируй; файлы считаются производными.

Q: Что если хочу посмотреть diff семантики?  
A: Запусти регенерацию дважды после правки — сравни содержимое локально (они же не в git, можно использовать `diff` / IDE).

---
Поддержка / улучшения: при расширении набора правил **сначала** добавь тесты (Golden), затем регенерация и commit.
