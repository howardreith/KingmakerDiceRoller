#!/usr/bin/env python3
"""Fail-fast source qualification for Kingmaker Dice Roller."""
from __future__ import annotations
import argparse
import ast
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = [
    'AGENTS.md','CHANGELOG.md','Directory.Build.props','GamePath.props.example','Info.json',
    'KingmakerDiceRoller.sln','LICENSE','PROJECT-STATE.md','README.md','THIRD-PARTY-NOTICES.md',
    'src/KingmakerDiceRoller/KingmakerDiceRoller.csproj',
    'src/KingmakerDiceRoller/CharacterCreation/MainCharacterIdentityRelation.cs',
    'src/KingmakerDiceRoller/CharacterCreation/GenerationRollbackSnapshot.cs',
    'src/KingmakerDiceRoller/CharacterCreation/LivePreviewInspector.cs',
    'src/KingmakerDiceRoller/CharacterCreation/LivePreviewObservation.cs',
    'src/KingmakerDiceRoller/CharacterCreation/PointBuyRestoreObservation.cs',
    'src/KingmakerDiceRoller/CharacterCreation/PointBuyPresentationObservation.cs',
    'src/KingmakerDiceRoller/CharacterCreation/AbilityPhasePresentationService.cs',
    'src/KingmakerDiceRoller/CharacterCreation/PointBuyOrigin.cs',
    'src/KingmakerDiceRoller/CharacterCreation/CharacterRollWorkflow.cs',
    'src/KingmakerDiceRoller/CharacterCreation/RollUiSnapshot.cs',
    'src/KingmakerDiceRoller/CharacterCreation/NativeAbilityControlService.cs',
    'src/KingmakerDiceRoller/CharacterCreation/RollSessionMode.cs',
    'tests/KingmakerDiceRoller.DomainTests/KingmakerDiceRoller.DomainTests.csproj',
    'tests/KingmakerDiceRoller.DomainTests/CharacterCreationContextPolicyTests.cs',
    'tests/KingmakerDiceRoller.DomainTests/PreviewSessionContinuityTests.cs',
    'scripts/Common.ps1','scripts/Initialize-GamePath.ps1','scripts/Validate-Repository.ps1',
    'scripts/Test-SourceOracle.ps1','scripts/Test-Domain.ps1','scripts/Verify-KingmakerContracts.ps1',
    'scripts/Build-Local.ps1','scripts/Package.ps1','scripts/Validate-Package.ps1','scripts/Install.ps1','scripts/Uninstall.ps1',
    'scripts/Qualify.ps1','scripts/Collect-RuntimeEvidence.ps1',
    'docs/ARCHITECTURE.md','docs/INTEGRATION-SEAMS.md','docs/COMPATIBILITY.md',
    'docs/SMOKE-TEST.md','docs/RUNTIME-DIAGNOSTICS.md','docs/BUILD-AND-RELEASE.md',
    'docs/SOURCE-QUALIFICATION.md','docs/UI-DESIGN.md','docs/LICENSING.md'
]
FORBIDDEN_BINARY_SUFFIXES = {'.dll','.exe','.pdb','.mdb','.zip','.zks','.sav','.png','.jpg','.jpeg','.dds','.asset','.bundle'}

class Failure(Exception): pass

def require(condition, message):
    if not condition: raise Failure(message)

def strip_csharp(text):
    out=[]; i=0; state='normal'
    while i < len(text):
        c=text[i]; n=text[i+1] if i+1<len(text) else ''
        if state=='normal':
            if c=='/' and n=='/': state='line'; out.extend('  '); i+=2; continue
            if c=='/' and n=='*': state='block'; out.extend('  '); i+=2; continue
            if c=='@' and n=='"': state='verbatim'; out.extend('  '); i+=2; continue
            if c=='"': state='string'; out.append(' '); i+=1; continue
            if c=="'": state='char'; out.append(' '); i+=1; continue
            out.append(c); i+=1; continue
        if state=='line':
            if c=='\n': state='normal'; out.append('\n')
            else: out.append(' ')
            i+=1; continue
        if state=='block':
            if c=='*' and n=='/': state='normal'; out.extend('  '); i+=2
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
        if state=='string':
            if c=='\\': out.extend('  '); i+=2
            elif c=='"': state='normal'; out.append(' '); i+=1
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
        if state=='char':
            if c=='\\': out.extend('  '); i+=2
            elif c=="'": state='normal'; out.append(' '); i+=1
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
        if state=='verbatim':
            if c=='"' and n=='"': out.extend('  '); i+=2
            elif c=='"': state='normal'; out.append(' '); i+=1
            else: out.append('\n' if c=='\n' else ' '); i+=1
    require(state in {'normal','line'}, f'unterminated C# token state: {state}')
    return ''.join(out)

def balanced(text, path):
    pairs={')':'(',']':'[','}':'{'}; stack=[]
    for offset,c in enumerate(text):
        if c in '([{': stack.append((c,offset))
        elif c in ')]}':
            require(stack and stack[-1][0]==pairs[c], f'{path}: delimiter mismatch at offset {offset}')
            stack.pop()
    require(not stack, f'{path}: unclosed delimiter {stack[-1][0] if stack else ""}')


def strip_powershell(text):
    require(not re.search(r'(?m)^\s*@(?:"|\')', text), 'PowerShell here-strings are not supported by the source validator')
    out=[]; i=0; state='normal'
    while i < len(text):
        c=text[i]; n=text[i+1] if i+1<len(text) else ''
        if state=='normal':
            if c=='<' and n=='#': state='block'; out.extend('  '); i+=2; continue
            if c=='#': state='line'; out.append(' '); i+=1; continue
            if c=="'": state='single'; out.append(' '); i+=1; continue
            if c=='"': state='double'; out.append(' '); i+=1; continue
            out.append(c); i+=1; continue
        if state=='line':
            if c=='\n': state='normal'; out.append('\n')
            else: out.append(' ')
            i+=1; continue
        if state=='block':
            if c=='#' and n=='>': state='normal'; out.extend('  '); i+=2
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
        if state=='single':
            if c=="'" and n=="'": out.extend('  '); i+=2
            elif c=="'": state='normal'; out.append(' '); i+=1
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
        if state=='double':
            if c=='`' and i+1<len(text): out.extend('  '); i+=2
            elif c=='"': state='normal'; out.append(' '); i+=1
            else: out.append('\n' if c=='\n' else ' '); i+=1
            continue
    require(state in {'normal','line'}, f'unterminated PowerShell token state: {state}')
    return ''.join(out)

def sha256(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda:f.read(1024*1024),b''): h.update(chunk)
    return h.hexdigest()

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--report', type=Path)
    args=parser.parse_args()
    checks=[]
    def ok(name): checks.append(name)

    for rel in REQUIRED: require((ROOT/rel).is_file(), f'missing required file: {rel}')
    ok(f'required files ({len(REQUIRED)})')

    info=json.loads((ROOT/'Info.json').read_text(encoding='utf-8'))
    require(info['Id']=='KingmakerDiceRoller','unexpected UMM ID')
    require(info['AssemblyName']=='KingmakerDiceRoller.dll','unexpected assembly name')
    require(info['EntryMethod']=='KingmakerDiceRoller.Main.Load','unexpected entry method')
    require(info['GameVersion']=='2.1.7','unexpected target game version')
    ok('Info.json identity')

    xml_files=list(ROOT.rglob('*.csproj'))+list(ROOT.rglob('*.props'))
    for path in xml_files: ET.parse(path)
    ok(f'XML parse ({len(xml_files)})')

    json_files=[p for p in ROOT.rglob('*.json') if '.git' not in p.parts and 'artifacts' not in p.parts]
    for path in json_files: json.loads(path.read_text(encoding='utf-8-sig'))
    ok(f'JSON parse ({len(json_files)})')

    python_files=[p for p in ROOT.rglob('*.py') if '.git' not in p.parts and 'artifacts' not in p.parts]
    for path in python_files: ast.parse(path.read_text(encoding='utf-8'), filename=str(path.relative_to(ROOT)))
    ok(f'Python parse ({len(python_files)})')

    powershell_files=list((ROOT/'scripts').glob('*.ps1'))
    for path in powershell_files:
        raw=path.read_text(encoding='utf-8')
        require('\r' not in raw, f'{path.relative_to(ROOT)} uses CRLF')
        balanced(strip_powershell(raw), path.relative_to(ROOT))
    ok(f'PowerShell lexical/balance audit ({len(powershell_files)})')

    tracked_candidates=[p for p in ROOT.rglob('*') if p.is_file() and '.git' not in p.parts and 'artifacts' not in p.parts]
    bad=[str(p.relative_to(ROOT)) for p in tracked_candidates if p.suffix.lower() in FORBIDDEN_BINARY_SUFFIXES]
    require(not bad, 'binary/game artifacts present: '+', '.join(bad))
    ok('no binary or game artifacts')

    csharp=list(ROOT.rglob('*.cs'))
    require(len(csharp)>=35, f'expected substantial C# source tree, found {len(csharp)} files')
    forbidden_patterns=[
        (r'\brecord\s+(?:(?:class|struct)\s+)?[A-Za-z_]\w*\s*(?:[({:])', 'records require newer C#'),
        (r'\binit\s*;', 'init accessors require newer C#'),
        (r'\busing\s+var\b', 'using declarations require newer C#'),
        (r'\bnamespace\s+[A-Za-z0-9_.]+\s*;', 'file-scoped namespace requires newer C#'),
        (r'\bnew\s*\(\s*\)', 'target-typed new requires newer C#'),
        (r'\bis\s+not\b', 'is not pattern requires newer C#')
    ]
    for path in csharp:
        raw=path.read_text(encoding='utf-8')
        require('\r' not in raw, f'{path.relative_to(ROOT)} uses CRLF in C# source')
        stripped=strip_csharp(raw)
        balanced(stripped, path.relative_to(ROOT))
        for pattern,label in forbidden_patterns:
            require(not re.search(pattern,stripped), f'{path.relative_to(ROOT)}: {label}')
    ok(f'C# lexical/balance/C#7.3 audit ({len(csharp)})')

    domain='\n'.join(p.read_text(encoding='utf-8') for p in (ROOT/'src/KingmakerDiceRoller/Domain').glob('*.cs'))
    for token in ['Kingmaker.','UnityEngine','UnityModManager','Harmony12']:
        require(token not in domain, f'pure domain leaks dependency: {token}')
    ok('pure domain dependency boundary')

    src='\n'.join(p.read_text(encoding='utf-8') for p in (ROOT/'src/KingmakerDiceRoller').rglob('*.cs'))
    stripped_src='\n'.join(strip_csharp(p.read_text(encoding='utf-8')) for p in (ROOT/'src/KingmakerDiceRoller').rglob('*.cs'))
    require('StatsDistributionStarted' in src and 'StatsDistributionIsComplete' in src and 'LevelUpStateConstructed' in src,'expected three patch bridge surfaces')
    for token in ['StatsDistribution.Add','StatsDistribution.Remove','StatsDistribution.CanAdd','StatsDistribution.CanRemove']:
        require(token not in src, f'forbidden broad allocator patch reference: {token}')
    controller=(ROOT/'src/KingmakerDiceRoller/Patches/KingmakerPatchController.cs').read_text(encoding='utf-8')
    require(controller.count('PatchPostfix(candidate,')==3,'patch controller must install exactly three postfixes')
    require('Priority.VeryLow' in controller,'patch priority must be explicit')
    for token in ['BlueprintScriptableObject','BlueprintBuff','BlueprintFeature','UnitPart','.AddFact(','SaveGame']:
        require(token not in stripped_src, f'save-owned custom content surface is forbidden: {token}')
    ok('narrow Harmony patch surface')

    diagnostic=(ROOT/'src/KingmakerDiceRoller/Domain/DiagnosticArrays.cs').read_text(encoding='utf-8')
    require(re.search(r'16\s*,\s*15\s*,\s*14\s*,\s*12\s*,\s*10\s*,\s*8',diagnostic),'fixed diagnostic array missing')
    restore=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/PointBuyRestoreService.cs').read_text(encoding='utf-8')
    point_buy_origin=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/PointBuyOrigin.cs').read_text(encoding='utf-8')
    rollback=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/GenerationRollbackSnapshot.cs').read_text(encoding='utf-8')
    restore_observation=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/PointBuyRestoreObservation.cs').read_text(encoding='utf-8')
    presentation=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/AbilityPhasePresentationService.cs').read_text(encoding='utf-8')
    presentation_observation=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/PointBuyPresentationObservation.cs').read_text(encoding='utf-8')
    stat_access=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/KingmakerStatAccess.cs').read_text(encoding='utf-8')
    require('PointBuyOrigin pristine = session.PointBuyOrigin' in restore and 'new object[] { pristine.AllocatorBudget }' in restore and 'DistributionStartMethod.Invoke' in restore,'point-buy restore must use the exact pre-roll point-buy origin budget')
    require('preview.Refresh(contracts)' in restore and 'pristine.Restore(session.Distribution, session.Unit' in restore,'point-buy restore must refresh once and restore pristine state on the newest session generation')
    require('HybridStateDetected' in restore and 'rolled-values-plus-full-budget hybrid' in restore,'point-buy restore must reject the observed hybrid state')
    require('25' not in restore,'point-buy restore may not hard-code 25 points')
    require('capturedGeneration <= 0' in point_buy_origin and 'CapturedBeforeRollOwnership' in point_buy_origin,'point-buy origin must carry positive-generation pre-roll provenance')
    for token in ['AllocatorBudget','BudgetSource','AllocatorAvailable','RemainingPoints','TotalPoints']:
        require(token in point_buy_origin, f'point-buy origin missing: {token}')
    require('GenerationRollbackSnapshot' in rollback and 'public int Generation' in rollback and 'MatchesAssignment' in rollback,'generation-local rollback state missing')
    require('RolledAssignmentStillPresent' in restore_observation and 'FullAllocatorBudgetAvailable' in restore_observation and 'HybridStateDetected' in restore_observation,'hybrid restoration verification missing')
    require('DisablePointBuyAllocator' in stat_access and 'DistributionAvailableMember' in stat_access and 'DistributionPointsMember' in stat_access,'roll mode must explicitly suppress the point-buy allocator')
    require('AbilityAllocatorFillDataMethod.Invoke(allocator, null)' in presentation,'point-buy presentation must invoke the exact native ability allocator refresh')
    require('refreshCountForGeneration == 0' in presentation and 'refreshInProgress' in presentation and 'nested refresh was refused' in presentation,'native presentation refresh must be bounded and reject reentrancy')
    require('session.IsPointBuyMode' in presentation and 'TrySynchronizeRoll' in presentation,'presentation service must separately synchronize PointBuy and Roll modes')
    require('PreviewUpdateMethod' not in presentation and 'PreviewRefreshService' not in presentation,'presentation synchronization must not rebuild the semantic preview')
    for token in [
        'semanticPointBuyVerified','presentationRefreshRequested','presentationRefreshMethod',
        'presentationRefreshCount','activeAbilityPhaseFound','abilityPhaseStateMatchesSession',
        'abilityPhaseDistributionMatchesSession','abilityPhaseViewModelMatchesSession',
        'postRefreshGeneration','postRefreshLiveModelVerified','mode=',
        'rollSuppressedForStableOwner'
    ]:
        require(token in presentation_observation, f'point-buy presentation diagnostic missing: {token}')
    context=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/CharacterCreationContextPolicy.cs').read_text(encoding='utf-8')
    for token in ['CharGen','IsFirstLevel','IsMainCharacter','IsPlayerFaction','IsPet','IsPlayersEnemy']:
        require(token in context, f'context guard missing: {token}')
    require('ObserveActiveController' in context and 'LevelUpController' in context,'active controller ownership guard missing')
    require('TryGetMainCharacterDescriptor' in context and 'Player.MainCharacter resolves to a different UnitDescriptor' in context,'new-game main-character boundary guard missing')
    relation=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/MainCharacterIdentityRelation.cs').read_text(encoding='utf-8')
    for token in ['Unresolved','Absent','SameAsCandidate','SameAsControllerUnit','DifferentFromCandidate']:
        require(token in relation, f'main-character relation missing: {token}')
    require('ReferenceEquals(mainDescriptor, candidateDescriptor)' in relation,'same-candidate preview identity exception missing')
    require('ReferenceEquals(mainDescriptor, controllerUnitDescriptor)' in relation,'controller source/preview identity bridge missing')
    require('mainCharacterResolved' in relation and 'controllerUnitResolved' in relation,'unresolved main-character identity must fail closed')
    liveness=(ROOT/'src/KingmakerDiceRoller/Domain/SessionLivenessTracker.cs').read_text(encoding='utf-8')
    manager=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/RollSessionManager.cs').read_text(encoding='utf-8')
    session=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/RollSession.cs').read_text(encoding='utf-8')
    decision=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/CharacterCreationContextDecision.cs').read_text(encoding='utf-8')
    application=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/StatApplicationService.cs').read_text(encoding='utf-8')
    live_preview=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/LivePreviewObservation.cs').read_text(encoding='utf-8')
    preview_refresh=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/PreviewRefreshService.cs').read_text(encoding='utf-8')
    coordinator=(ROOT/'src/KingmakerDiceRoller/CharacterCreation/CharacterCreationCoordinator.cs').read_text(encoding='utf-8')
    contracts=(ROOT/'src/KingmakerDiceRoller/Integration/KingmakerContractResolver.cs').read_text(encoding='utf-8')
    composition=(ROOT/'src/KingmakerDiceRoller/CompositionRoot.cs').read_text(encoding='utf-8')
    main_source=(ROOT/'src/KingmakerDiceRoller/Main.cs').read_text(encoding='utf-8')
    require('UnconfirmedGraceSeconds' in liveness and 'ConfirmedGraceSeconds' in liveness,'session liveness grace policy missing')
    require('ReleaseIfStableOwnerLost' in manager and 'OwnsStableOwner(currentController, currentSourceUnit)' in manager and 'Lifecycle.Abandon' in manager,'stable-owner session release missing')
    require('public object Controller { get; }' in session and 'public object StableOwner { get; }' in session,'immutable stable controller/source ownership missing')
    require('public object Unit { get; private set; }' in session and 'int nextGeneration = Generation + 1' in session and 'Generation = nextGeneration' in session,'replaceable preview generation missing')
    require('public PointBuyOrigin PointBuyOrigin { get; private set; }' in session and 'public GenerationRollbackSnapshot GenerationRollback { get; private set; }' in session,'point-buy origin and generation rollback must have distinct lifetimes')
    rebind=session[session.find('public void Rebind('):]
    require('GenerationRollback = generationRollback' in rebind and 'PointBuyOrigin =' not in rebind,'same-owner rebind must replace rollback state without replacing the pre-roll point-buy origin')
    require('RollSessionMode.EnteringRollMode' in session and 'RollSessionMode.RestoringPointBuy' in session and 'RollSessionMode.PointBuy' in session and 'RollSuppressedForStableOwner' in session,'explicit product workflow modes are missing')
    require('OwnsStableOwner(context.Controller, context.StableOwner)' in manager and 'different controller/source owner' in manager,'same-owner rebind/different-owner rejection missing')
    require('Opened a PointBuy-first session' in manager and 'no roll was generated' in manager,'new sessions must open in PointBuy without automatic random generation')
    require('rollbackFactory(replacementGeneration)' in manager,'generation rollback state must follow preview generations')
    require('StableOwner' in decision and 'ControllerPreviewMatches' in decision,'accepted context must retain stable and transient controller identities')
    require('TryStageCurrentGeneration' in application and '.Refresh(' not in application,'fixed-array staging must not recursively request preview refresh')
    require('TryMarkLiveVerified' in application and 'InspectLive' in application,'application must verify the live controller generation')
    require('DisablePointBuyAllocator' in application and 'ReadDistributionPoints' in application,'roll staging must disable and verify the point-buy allocator')
    for token in ['applicationGeneration','refreshInProgress','pendingReplacementObserved','sameStableOwner','reboundPreview','currentControllerStateMatches','currentControllerPreviewMatches','liveDistributionMatches','liveUnitValuesMatch','liveAllocatorMatches']:
        require(token in live_preview, f'live preview diagnostic missing: {token}')
    for token in ['pointBuyOriginCaptured','pristineBaselineGeneration','currentGeneration','candidateBaselineContaminated','mode=','allocatorBudget','liveDistributionMatchesPristine','liveUnitMatchesPristine','rollSuppressedForStableOwner']:
        require(token in restore_observation or token in coordinator, f'point-buy diagnostic missing: {token}')
    require('refreshInProgress' in preview_refresh and 'nested refresh was refused' in preview_refresh and 'finally' in preview_refresh,'bounded preview refresh guard missing')
    require('TryMarkLiveVerified' in coordinator and 'MaximumApplicationAttemptsPerGeneration' in coordinator,'bounded post-constructor live verification missing')
    require('if (!session.IsRollMode) return;' in coordinator and 'no array was generated or staged' in coordinator,'PointBuy mode must suppress roll staging and completion behavior')
    require('TryRoll(out string error)' in coordinator and 'TryReroll(out string error)' in coordinator and 'TryApplyUserAssignment' in coordinator,'explicit transactional roll command surface missing')
    require('TryPrepareDisable' in coordinator and 'TryPrepareDisable' in composition,'disable must restore point buy before removing recovery hooks')
    require('pointBuyPresentation.TrySynchronize' in coordinator and 'Pristine point-buy model is verified and durable' in coordinator,'coordinator must distinguish safe semantic restoration from presentation failure')
    require('error = null;' in coordinator[coordinator.find('bool presentationSynchronized'):coordinator.find('public bool TryPrepareDisable')],'presentation failure must preserve successful semantic point-buy restoration')
    require('RequireInstanceMember(controllerType, "State")' in contracts,'LevelUpController.State contract guard missing')
    require('ReflectionAccess.CanWrite(available)' in contracts and 'ReflectionAccess.CanWrite(points)' in contracts and 'ReflectionAccess.CanWrite(totalPoints)' in contracts,'allocator state contracts must be proven writable before patching')
    require('RequireInstanceMember(controllerType, "Unit")' in contracts and 'RequireInstanceMember(controllerType, "Preview")' in contracts,'controller identity contracts missing')
    require('RequireInstanceMember(playerType, "MainCharacter")' in contracts and 'UnitReference.Value' in contracts,'Player.MainCharacter normalization contract missing')
    for token in [
        'Kingmaker.UI.LevelUp.Phase.CharBPhase+Type',
        'Kingmaker.UI.LevelUp.Phase.CharBPhaseSkills',
        'Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator',
        'RequireInstanceMember(characterBuildControllerType, "CurrentPhase")',
        'RequireInstanceMember(characterBuildControllerType, "Skills")',
        'RequireInstanceMember(skillsPhaseType, "AbilityScoresAllocator")',
        'GetMethod(', '"FillData"', 'GetField("m_Unit"', 'GetField("m_PreviewUnit"'
    ]:
        require(token in contracts, f'exact native ability presentation contract missing: {token}')
    require('modEntry.OnUpdate = OnUpdate' in main_source,'UMM update lifecycle hook missing')
    ok('fixed-array, restoration, context, and stale-session invariants')

    tests=(ROOT/'tests/KingmakerDiceRoller.DomainTests/Program.cs').read_text(encoding='utf-8')
    test_count=tests.count('new TestCase(')
    require(test_count>=123,f'expected at least 123 C# behavior cases, found {test_count}')
    context_tests=(ROOT/'tests/KingmakerDiceRoller.DomainTests/CharacterCreationContextPolicyTests.cs').read_text(encoding='utf-8')
    for token in [
        'NoMainCharacterValuePermitsCandidate','DirectSameMainCharacterPermitsCandidate',
        'ControllerSourceMainCharacterPermitsOwnedPreview','DifferentMainCharacterIsRejected',
        'UnresolvableMainCharacterFailsClosed','RespecRemainsRejectedWhenMainMatches',
        'NonFirstLevelRemainsRejectedWhenMainMatches','PetCandidateRemainsRejected',
        'EnemyCandidateRemainsRejected','ControllerOwnershipRemainsMandatory',
        'DifferentMainCharacterCannotOpenSession','RebuiltStateReusesFixedAssignment',
        'DiagnosticRelationDistinguishesSameAndDifferent'
    ]:
        require(token in context_tests, f'context regression behavior missing: {token}')
    continuity_tests=(ROOT/'tests/KingmakerDiceRoller.DomainTests/PreviewSessionContinuityTests.cs').read_text(encoding='utf-8')
    for token in [
        'PreviewAOpensWithStableSource','PreviewBRebindsWithDifferentDescriptor',
        'SameOwnerDoesNotReportAnotherUnit','ConstructorStageReplacementIsMarkedPending',
        'DifferentStableOwnerIsRejected','AssignmentSurvivesThreeGenerations',
        'RebindReplacesTransientObjectsAndRollbackButPreservesPristine',
        'FirstPreviewCapturesPristinePointBuyOrigin','FixedStagingDoesNotMutatePristineOrigin',
        'SameOwnerRebindNeverRecapturesPristineOrigin','GenerationRollbackChangesIndependentlyFromPristineOrigin',
        'NestedPreviewRefreshIsRefused',
        'ReentrantReplacementUsesOneRefresh','FinalLiveReplacementContainsFixedArray',
        'ApplicationDoesNotRequestAnotherRefresh','CoordinatorCountsOnlyVerifiedLiveApplication',
        'DetachedMatchingPreviewCannotVerify',
        'SameOwnerReplacementDoesNotRelease','NullStateWithSameOwnerDoesNotRelease',
        'MissingControllerEventuallyReleases','DifferentControllerEventuallyReleases',
        'PointBuyRestoresNewestPreviewOnly','PointBuyRestoresNonDefaultBudgetAndAllocation',
        'HybridRolledValuesAndFullBudgetCannotVerify',
        'ZeroBudgetPristineAssignmentIsNotMisclassifiedAsHybrid',
        'RacialModifiersRemainSeparateFromRestoredBaseValues',
        'PointBuyModeSurvivesSameOwnerRebuildWithoutRestaging',
        'PointBuyModeDoesNotForceCompletionOrAllocatorRestart',
        'DisableDuringRollRestoresBeforeClearingOwnership',
        'FailedRestorationRollsBackToIsolatedRollMode','FailedRollbackRefusesUnsafeDisable',
        'PointBuyModeCancellationReleasesAndNewOwnerCanOpen','RestorationDiagnosticsExposePristineTransition',
        'CompletionUsesCurrentLiveDistributionOnly',
        'ExistingAndSpecialCreationPathsRemainExcluded','DiagnosticsDistinguishPreviewLifecycle',
        'SemanticRestoreWithoutPresentationIsNotSynchronized',
        'NativeAbilityRefreshRunsAfterPristineWrites','PresentationRefreshIsBoundedPerGeneration',
        'PresentationRefreshCannotReenterRollMode','SameOwnerReplacementDuringPresentationStaysSuppressed',
        'FixedAssignmentIsNotRestagedByPresentation','PostRefreshLiveStateRemainsPristine',
        'PostRefreshAllocatorKeepsObservedBudget','PresentationBindsCurrentStateAndDistribution',
        'HumanPresentationImmediatelyShowsPristinePointBuy','RaceModifiersRemainSeparateInImmediatePresentation',
        'NonDefaultBudgetReachesImmediatePresentation','NavigationAfterPresentationStaysInPointBuy',
        'PresentationFailurePreservesSafePointBuy','PresentationFailureNeverRollsBackToFixedArray',
        'DisableAfterSemanticRestorationRemainsSafe','DisableDuringRollSynchronizesBeforeClear',
        'PresentationRefreshDoesNotRebuildPreview','InactiveAbilityPhaseCannotClaimSynchronization',
        'DiagnosticsSeparateSemanticAndPresentationFailure','DiagnosticsReportNativePresentationVerification',
        'ViewBindingMismatchCannotClaimSynchronization'
    ]:
        require(token in continuity_tests, f'preview continuity behavior missing: {token}')
    python_tests=(ROOT/'tests/python/test_domain_reference.py').read_text(encoding='utf-8')
    python_count=len(re.findall(r'^\s+def test_',python_tests,re.MULTILINE))
    require(python_count>=25,f'expected at least 25 Python oracle cases, found {python_count}')
    ok(f'test inventories (C# {test_count}, Python {python_count})')

    package_validator=(ROOT/'scripts/Validate-Package.ps1').read_text(encoding='utf-8')
    installer=(ROOT/'scripts/Install.ps1').read_text(encoding='utf-8')
    common=(ROOT/'scripts/Common.ps1').read_text(encoding='utf-8')
    require('Duplicate package entry' in package_validator and 'exactly $($allowed.Count) files' in package_validator,'exact package allowlist validation missing')
    require('rollback was attempted' in installer and '.KingmakerDiceRoller.install.' in installer,'transactional install rollback missing')
    require('Security.Cryptography.SHA256' in common and 'Get-FileHash' not in common,'hash helper must not inherit WhatIf behavior')
    require('[IO.Directory]::CreateDirectory($temporary)' in installer and '[IO.Directory]::Delete($temporary, $true)' in installer,'WhatIf preflight temp lifecycle must execute outside ShouldProcess')
    require('function Assert-DirectoryExists' in common,'shared directory assertion helper missing')
    collector=(ROOT/'scripts/Collect-RuntimeEvidence.ps1').read_text(encoding='utf-8')
    require("Pathfinder Kingmaker\\output_log.txt" in collector,'runtime evidence collector must include the live LocalLow output log')
    ok('package allowlist and transactional installation guards')

    notices=(ROOT/'THIRD-PARTY-NOTICES.md').read_text(encoding='utf-8')
    upstream=(ROOT/'licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt').read_text(encoding='utf-8')
    require('FakeFriend24/wotr-dice-roller' in notices,'upstream attribution missing')
    require('JesusLives24' in upstream and 'Jennifer Messerly' in upstream,'upstream MIT notices incomplete')
    ok('licensing and attribution')

    state=(ROOT/'PROJECT-STATE.md').read_text(encoding='utf-8')
    for label in ['Implemented','Source-qualified','Build-qualified','Runtime-qualified','Compatibility-qualified']:
        require(label in state, f'qualification label missing: {label}')
    ok('qualification disclosure')

    report={
        'status':'passed','checks':checks,'csharp_files':len(csharp),'csharp_behavior_cases':test_count,
        'python_behavior_cases':python_count,'info_sha256':sha256(ROOT/'Info.json')
    }
    if args.report:
        target=args.report if args.report.is_absolute() else ROOT/args.report
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))
    return 0

if __name__=='__main__':
    try: raise SystemExit(main())
    except Failure as exc:
        print('SOURCE QUALIFICATION FAILED: '+str(exc),file=sys.stderr)
        raise SystemExit(1)
