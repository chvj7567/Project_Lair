# 던전 자유 편성 QA (기획서 §7) — 2026-11-23

기준 문서: `docs/design/dungeon-formation-free.md`
방식: 정적 코드 검토 + 기존 테스트 매핑. **이 에이전트는 UnityMCP 가 없어 어떤 테스트·시뮬도 직접 실행하지 않았다.** 아래 수치는 기존 결과 파일 인용뿐이며 새 시뮬 수치는 없다. 밸런스 수치(승률·처치시간 등)는 측정하지 못했다.

## 0. Evidence

- EditMode 결과: `Library/lair-test-result.json` → `"finishedAt": "2026-11-23T23:25:25"`, `"pass": 6820, "fail": 0, "skip": 0`. 메인이 오늘 돌린 결과로 보이며 나는 재실행하지 않았다.
- PlayMode 결과: `Library/lair-test-result-playmode.json` 은 2026-11-14 자료라 이번 편성 구현 이전이다. 유효한 근거가 아니다.
- 시뮬 인프라: `Lair/Tests/Run PlayMode Simulation` → `Assets/_Lair/Tests/PlayMode/BalanceSimulationTest.cs`. 전략은 Random 과 4축 우선(Tank/Dps/Debuff/Swarm)이고 `Enhance_<종족>` 레벨을 `MetaSession.Profile` 에 시드한다. **편성(`HasCustomFormation`/`Formation`)을 시드하는 코드는 없다.** 그래서 §7 의 편성별 비교는 현재 도구로 만들 수 없다.

## 1. (a) 시나리오별 기존 테스트 커버 매핑

| §7 시나리오 | 커버 | 근거(테스트 파일) |
|---|---|---|
| 0칸 편성의 저장·구별 | 커버 | `Formation/FormationMetaProfileTests.cs`: `NormalizeFormation은_수동_0칸_편성을_그대로_두고_플래그도_유지한다`, `Store_Save_Load_라운드트립은_수동_0칸_편성을_기본_편성과_구별해_보존한다`, `Store_Save_Load_라운드트립은_수동_부분_편성의_빈칸을_채우지_않는다` |
| 기본 편성 플래그가 남은 행을 비움 | 커버 | 같은 파일 `NormalizeFormation은_기본_편성_플래그면_남은_행을_비운다`, `마이그레이션과_NormalizeFormation은_멱등이다` |
| 클라우드 복원 시 플래그 복사 | 부분 | 같은 파일 `CopyFrom_*` 가 편성 행 복사를 본다. `MetaProfile.CopyFrom`(615행)에 플래그 복사는 있다. 플래그가 어긋나면 복원이 깨지는지를 직접 단언한 테스트는 확인하지 못했다. |
| v13 정상 6행이 수동으로 이행 | 부분 | `마이그레이션은_유효한_6행을_보존한다` 는 Version=8 입력이고 행 보존만 단언한다. v13 입력 후 `HasCustomFormation==true` 를 단언한 테스트는 확인하지 못했다. 부분 행·중복·미지 종·null 행이 정리되는 테스트는 있다. |
| 수동 편성은 승급 후 유지, 기본만 최고 등급 추적 | 커버 | `Formation/MonsterGradeSmokeTests.cs`: `견습_승급은_소울을_내고_보유_등급이_오르며_수동_편성은_유지하고_기본_편성만_최고_등급을_따라간다` |
| 카드 추첨이 편성과 독립 | 커버 | `Formation/FormationCardExclusionTests.cs`: `편성_상태를_바꿔도_제외_판정은_변하지_않는다` (`BattleController.IsCardExcluded` 는 `counter.IsCapped(id)` 만 본다) |
| 봉인 첫 순위·대체·해제 | 레거시 위주 | `FormationCardAndSealTests.cs` 의 봉인 테스트는 `StageRuleContext.LockSpawners` 의 "1곳 봉인", "최소 슬롯", "후보 없음", "봉인된 둥지 제외", 해제를 다룬다. **빈 둥지(null)와 새 폴백 순서(원래 기본 위치 → 최저 채움)를 `PickSealedSlot(EMonster?[]…, db)` 로 직접 검증한 테스트는 확인하지 못했다.** |
| 상점 T1/T2/T3 가격·재화 부족·구매 | 커버 | `Meta/SpeciesShopPriceTests.cs`: 순차 구매 대표값 2/4/5 와 누계 2371, 389 소울 불가·부족분 1, 390 소울 구매, 단계 데이터 없음 시 구매 비활성(소울 미차감), 6종 공유. T3 최대는 `T3_최대_티어면_가격이_없고_구매가_차단된다` |
| 구레벨 Lv1~5 이행 (1→2, 3→4, 중복, 음수, 멱등, 소울 불변) | **미커버** | `MonsterEnhanceTierRules.NormalizeProfile` 을 이름으로 호출하는 테스트가 없다(`Tests` 에서 grep 0건). `MonsterEnhancementVisualTierWiringTests.Lv3은_티어2_해당_HP바높이를_적용한다` 같은 표시 테스트만 있다. |
| 이동/교환/비우기/배치 (`FormationViewModel`) | **미커버** | `Tests` 에서 `FormationViewModel` 참조 0건. `Place`/`ClearNest`/`ResetToDefault`/`BuildSaveRows`/`SealForecastText` 가 무검증이다. |
| 챕터·탑 저장 편성 vs 데일리 기본 편성 | **미커버** | `ResolveFormation(profile, _spec.IsDaily)`(`BattleController.cs:213`) 와 `FormationSave.Resolve(useDefault)` 에 대한 테스트 0건. |
| 빈 둥지의 전투 반영 (`ApplyFormation` → `ClearOutput`, 소환 없음, 6종 분산/집중/빈칸 비교) | **미커버** | EditMode 에 없고, PlayMode `BalanceSimulationTest` 도 편성을 시드하지 않는다. |
| UI 포커스·해상도·상태 문구 | 부분 | `FormationUi/FormationUiArtTests.cs`, `Formation/FormationPrefabLabelTests.cs`, `FormationAssetWiringTests.cs`, `UI/ResolutionAdaptationTests.cs` 는 배선·라벨·해상도다. 이동/교환 후 포커스 유지는 코드 호출로 검증할 수 없다. |

### 주의: 레거시 테스트가 죽은 코드를 검증한다

- `FormationCardAndSealTests.cs` 의 카드 테스트는 `FormationCardSpecies.IsAvailable` 로 "종이 없으면 카드 제외"를 단언한다(예: `종_의존_행이_없는_카드는_…`, `최악_편성에서_9픽을_진행해도_…`).
- 그런데 전투 추첨은 `IsCardExcluded` 가 픽 상한만 본다(`BattleController.cs:2270`, 추첨 호출부 2414·2429).
- `FormationCardSpecies` 는 `ChapterDatabaseLoader` 가 로드할 뿐 전투에서 쓰이지 않는다(`Scripts` grep 상 호출부는 로더 466/514/519행과 정의뿐).
- 이 테스트들이 통과해도 §4 "추첨은 편성과 독립"의 증거가 아니다. 증거는 `FormationCardExclusionTests` 3건뿐이다. 레거시 테스트는 기획서와 반대되는 설계를 고정하므로 정리 대상이다(수정은 test-engineer).
- 비슷하게 `FormationSaveTests` 의 `FromSaved는 null_0행_5행_7행에서_null이다` 는 "정확히 6행" 레거시 계약이다. 현재 `FromSaved` 는 v13 이행 판정에만 쓰이므로 의도와 맞지만, 새 `ReadCustom` 의 0~6행 계약과는 별개다.

## 2. (b) 정적 검토 — 규칙 vs 기획서

| 규칙 | 판정 | 근거 |
|---|---|---|
| 동일 몬스터·등급은 한 곳 | 일치 | `FormationViewModel.Place` 가 이미 다른 칸에 있으면 이동/교환(`Array.IndexOf` 후 `_formation[previous]=displaced`)해 중복을 만들지 않는다. `FormationSave.ReadCustom` 은 `used` 로 중복을 막고 낮은 슬롯만 남긴다. |
| 보유 등급만 배치 | 일치 | `FormationRules.IsAllowed`: `GradeOf(monster) <= OwnedGrade(race)`. 종·슬롯 고정 제한은 없다(자유 배치). |
| 슬롯 범위 밖·중복 슬롯·미지 종 | 일치 | `ReadCustom` 이 `entry.Slot != i+1` 이면 건너뛰고, 같은 슬롯은 먼저 읽은 유효 행을 쓴다. `repaired = used.Count != rows.Count` 로 복구 안내를 낸다. |
| 빈칸을 기본 몬스터로 채우지 않음 | 일치 | `ReadCustom` 은 `EMonster?[]` 이고 무효 칸은 null 로 둔다. 전투는 `ApplyFormation` 에서 null 이면 `ClearOutput()`, 슬롯 번호는 유지한다. |
| 봉인 첫 순위 | 일치 | `PickSealedSlot(EMonster?[], species, db, isCandidate)` 가 채워지고 미봉인인 최저 번호의 같은 종(모든 등급)을 즉시 반환한다. |
| 봉인 폴백 | 일치 | 대상 종이 없으면 기본 위치(`db.SlotOf(race)`)가 채움·미봉인일 때 그 칸, 아니면 첫 채움·미봉인 칸, 없으면 -1(봉인 없음). 편집 팝업(`SealedIndex`)과 전투(`StageRuleContext.LockSpawners`)가 같은 함수를 쓴다. 단 팝업은 `isCandidate` 없이, 전투는 `IsLocked==false` 를 넘긴다. 편성 시점 첫 봉인 1곳에서는 동일하다. |
| 카드 추첨 독립 | 일치 | `IsCardExcluded` 는 `counter.IsCapped(id)` 만 본다. 종·빈칸·봉인과 무관하고 난수도 쓰지 않는다. |
| 효과 대상 없음 안내(해당 효과만) | 확인 불가 | `HasOccupiedNest` / `HasMonsterEffectTarget` 가 존재한다. 복합 카드 효과별 분리 안내의 실제 동작은 Play 확인이 필요하다. |
| 저장 v14 | 일치 | `CurrentVersion = 14`. `NormalizeFormation` 은 `Version<14` 일 때 `FromSaved(rows)!=null` 로 플래그를 정하고(정상 6행=수동, 0행·비정상=기본), 이후 플래그가 false 면 행을 비운다. 이 호출이 버전 승격(54행)보다 앞서므로 이행 판정이 맞다. |
| 멱등·소울 불변 (티어) | 코드상 일치 | `NormalizeProfile` 은 같은 `ItemId` 중복 중 최고 티어를 남기고 `LevelEquivalent(tier)` 로 설정한다. 소울을 건드리지 않는다. 다만 테스트가 없다(§1). |
| 데일리=기본 편성, 챕터·탑=저장 편성 | 일치 | `ResolveFormation(profile, _spec.IsDaily)` → `useDefault` 면 `DefaultSlots`. 탑·챕터는 `IsDaily=false` 라 저장 편성이다. |
| 승급이 수동 편성을 바꾸지 않음 | 일치 | `GradePromotion` 은 `Formation` 을 건드리지 않는다. 기본 편성은 읽을 때 `DefaultSlots` 가 최고 등급을 따른다. |
| 잠금(1-1 클리어 전) | 일치 | `Resolve` 가 `IsUnlocked==false` 면 기본 편성을 쓴다. 팝업 표시는 `ShouldShowPopup` 이 같은 조건을 본다. |

의심 지점(코드 변경 제안이 아니라 확인 요청)
1. `FormationSave.ToSaved(IReadOnlyList<EMonster?>, …)` 가 기본과 같으면 0행·다르면 채워진 행만 쓴다. 기본 편성과 동일한 상태를 `isCustom=false` 로 저장해야 승급 추적이 유지된다. `VillageController.SortieWithFormation` 이 `viewModel.IsCustom` 을 넘기는지는 Play 에서 확인하자.
2. 팝업의 `SealedIndex` 는 편집 중 편성 기준이고 전투는 출력된 스포너 기준이다. 유물·카드가 시작 직후 출력을 바꾸지 않는 한 같다.

## 3. (c) 헤드리스로 비어 있는 시나리오와 Play 절차 제안

헤드리스로 못 채운 항목
1. §7 첫 항목: 6종 분산 vs 한 종 3~5칸 집중 vs 빈칸 혼합 × 봉인 유무의 승률·승리판 중앙 처치시간·종별 카드 적용 둥지 수·스폰 캡 도달 시간·선택 카드 종류. 시뮬이 편성을 시드하지 못한다. 시뮬 헬퍼에 `HasCustomFormation`/`Formation` 시드를 추가하는 일이 먼저다(gameplay-programmer 요청 필요).
2. §7 둘째 항목: 같은 난수·선택 이력에서 0/1/6칸·봉인 여부만 바꿔 카드 제안이 같은지. 같은 시드의 `deck.Draw` 결과를 비교하는 EditMode 테스트로 헤드리스 가능하지만 아직 없다.
3. 구레벨 Lv1~5 이행 테스트, `FormationViewModel` 이동/교환/비우기 테스트, 데일리 기본 vs 챕터 저장의 `ResolveFormation` 테스트, 봉인 폴백(빈 둥지 포함) 테스트. 모두 순수 로직이라 EditMode 로 가능하다(test-engineer).
4. 새 PlayMode 결과: 2026-11-14 이후 실행분이 없다.

메인이 Play 로 수행할 절차(Rule 05, `onClick` 호출 도구 사용)
1. 1-1 클리어 전 프로필: 방어하기 → 팝업 없이 기본 편성 출격. 전투 HUD 6칸이 기본 종과 같은지 본다.
2. 1-1 클리어 프로필: 방어하기 → 팝업이 열린다. 아래를 차례로 수행하고 문구와 `N / 6 배치` 를 확인한다.
   - 빈 둥지로 이동: ‘N번 둥지의 몬스터를 M번으로 옮겼어요’
   - 찬 둥지와 교환
   - 이 둥지 비우기(빈 둥지에서는 버튼 비활성)
   - 모두 비우기 → 0칸 안내 → 출격
3. 0칸 출격: 전투가 시작되고 자동 소환이 없다. 카드창이 정상으로 뜬다(30초 액티브, HP 10% 패시브). 시간 정지가 풀리는지 확인한다.
4. 분산 / 한 종 집중(한 종을 여러 등급으로 최대 5칸) / 빈 둥지 혼합으로 각각 출격 후 스포너 HUD·제단·필드 몬스터 종을 확인한다. 빈 칸이 도깨비불로 보이면 안 된다. 같은 영웅·난수를 쓰도록 데일리 시드나 고정 시드 판을 이용한다.
5. 봉인 규칙이 있는 스테이지(SealedNest)로 아래를 팝업의 ‘봉인 예정’ 표시와 전투 시작 후 실제 잠긴 둥지로 비교한다.
   - 대상 종이 2곳(예: 2번·5번) → 2번이 잠긴다.
   - 대상 종이 없고 그 기본 위치가 채워짐 → 그 위치가 잠긴다.
   - 기본 위치가 비었음 → 최저 번호 채운 둥지가 잠긴다.
   - 전부 비움 → 봉인 없음, 팝업은 ‘봉인할 둥지가 없어요’.
6. 카드 안내: 역병귀 둥지가 없는 편성에서 ‘역병귀 소환수 증가’ 카드를 선택해 해당 효과만 ‘이 효과를 받을 둥지가 없어요’ 류로 안내되는지 본다. 도깨비불·망령 공격력 같은 복합 카드에 ‘카드 전체 효과 없음’이 붙지 않는지도 본다. 카드 3장이 항상 채워지는지 본다.
7. 모드: 탑 팝업 → 안내 ‘마지막으로 저장한 던전 편성을 사용합니다’와 실제 출격 편성이 저장 편성인지. 데일리 → ‘데일리는 종족별 기본 편성을 사용합니다’와 기본 편성인지.
8. 저장·승급: 수동 편성(빈칸 포함) 저장 → 같은 종 승급 → 팝업을 다시 열면 빈칸·몬스터가 그대로이고 새 등급이 보유 목록에만 추가되는지. 기본 편성 상태로 출격한 뒤 승급하면 새 최고 등급을 따라가는지.
9. 닫기·뒤로가기·Esc 는 편집을 버리고 ‘출격’ 만 저장하는지. 닫은 뒤 ‘방어하기’ 에 포커스가 돌아오는지.
10. 상점 티어: T1/T2/T3 구매 시 소울 차감이 한 번이고, 같은 종 모든 등급 칩이 같은 티어를 보이는지. 편성 둥지·보유 목록 칩이 ‘기본/T1/T2/T3’ 로 보이는지. 구레벨 세이브(Lv1·Lv3) 파일을 넣고 로드해 T1·T2 로 올라가고 소울이 변하지 않는지.
11. 클라우드: 수동 0칸 편성을 올린 뒤 복원해 0칸이 그대로인지. 구버전 6행 백업 복원이 수동 편성인지.
12. 해상도 1280×720 / 1280×800 / 가로 모바일에서 칩·봉인·비우기 안내가 잘리지 않는지. 스크린샷을 ui-visual-reviewer 에 넘긴다.

## 4. 요약

- 정적 검토에서 기획서와 어긋난 규칙은 찾지 못했다(동일 몬스터·등급 한 곳, 봉인 폴백, 추첨 독립, v14 이행, 모드별 편성 모두 코드상 일치). 다만 실행 증거는 EditMode 6820 pass / 0 fail 한 줄이다.
- 테스트가 비어 있는 곳은 `FormationViewModel`, 구레벨 이행 `NormalizeProfile`, 데일리 vs 저장 편성 분기, 빈 둥지 포함 봉인 폴백, `ApplyFormation` 의 빈 둥지 처리다. 후속 담당은 test-engineer 다.
- 레거시 `FormationCardSpecies` 계열 테스트는 폐기된 규칙을 고정하고 있어 정리가 필요하다.
- §7 의 밸런스 비교(분산/집중/빈칸 × 봉인)는 시뮬이 편성을 시드하지 못해 수행하지 못했다. 시뮬 확장은 gameplay-programmer 요청 사항이다.

재현: EditMode 는 `editor_execute_menu("Lair/Tests/Run EditMode Tests")` 후 `Library/lair-test-result.json`. 시뮬은 `Lair/Tests/Run PlayMode Simulation`(현재 편성 미지원).
