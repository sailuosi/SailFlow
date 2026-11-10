# D4: bulk-replace entity PK .Id accessors (heuristic; excludes biz/AI)
$roots = @(
    "C:\sleveen\code\gitee\sailflow\src\core",
    "C:\sleveen\code\gitee\sailflow\src\biz\query",
    "C:\sleveen\code\gitee\sailflow\src\biz\host",
    "C:\sleveen\code\gitee\sailflow\src\tests",
    "C:\sleveen\code\gitee\sleveen-admin\backend\saiflow"
)
$excludeDir = [regex]::Escape("biz\AI")

$rules = @(
    # Join lambdas (explicit)
    @{ Pat = 'ai\.ProcessInstanceId == pi\.Id'; Rep = 'ai.ProcessInstanceId == pi.ProcessInstanceId' }
    @{ Pat = 't\.ActivityInstanceId == ai\.Id'; Rep = 't.ActivityInstanceId == ai.ActivityInstanceId' }
    @{ Pat = 't\.ProcessInstanceId == pi\.Id'; Rep = 't.ProcessInstanceId == pi.ProcessInstanceId' }
    @{ Pat = 'ai\.MainActivityInstanceId == mai\.Id'; Rep = 'ai.MainActivityInstanceId == mai.ActivityInstanceId' }
    @{ Pat = 't\.ToActivityInstanceId == a\.Id'; Rep = 't.ToActivityInstanceId == a.ActivityInstanceId' }
    @{ Pat = 'ai\.Id == t\.ActivityInstanceId'; Rep = 'ai.ActivityInstanceId == t.ActivityInstanceId' }
    @{ Pat = 'ai\.Id == ti\.ToActivityInstanceId'; Rep = 'ai.ActivityInstanceId == ti.ToActivityInstanceId' }
    @{ Pat = 'ai\.Id == ti\.FromActivityInstanceId'; Rep = 'ai.ActivityInstanceId == ti.FromActivityInstanceId' }

    # Instance / join aliases
    @{ Pat = '\bpi\.Id\b'; Rep = 'pi.ProcessInstanceId' }
    @{ Pat = '\bai\.Id\b'; Rep = 'ai.ActivityInstanceId' }
    @{ Pat = '\bmai\.Id\b'; Rep = 'mai.ActivityInstanceId' }
    @{ Pat = '\ba\.Id\b'; Rep = 'a.ActivityInstanceId' }
    @{ Pat = '\bprocessInstance\.Id\b'; Rep = 'processInstance.ProcessInstanceId' }
    @{ Pat = '\bactivityInstance\.Id\b'; Rep = 'activityInstance.ActivityInstanceId' }
    @{ Pat = '\bjoinNode\.Id\b'; Rep = 'joinNode.ActivityInstanceId' }
    @{ Pat = '\bsourceInst\.Id\b'; Rep = 'sourceInst.ActivityInstanceId' }
    @{ Pat = '\bfromActInst\.Id\b'; Rep = 'fromActInst.ActivityInstanceId' }
    @{ Pat = '\btoActInst\.Id\b'; Rep = 'toActInst.ActivityInstanceId' }
    @{ Pat = '\bmainActivityInstance\.Id\b'; Rep = 'mainActivityInstance.ActivityInstanceId' }
    @{ Pat = '\bcurrentActivityInstance\.Id\b'; Rep = 'currentActivityInstance.ActivityInstanceId' }
    @{ Pat = '\bbackSrcActivityInstance\.Id\b'; Rep = 'backSrcActivityInstance.ActivityInstanceId' }

    # Process definition
    @{ Pat = '\bprocessEntity\.Id\b'; Rep = 'processEntity.ProcessDefId' }
    @{ Pat = '\bpackageDB\.Id\b'; Rep = 'packageDB.ProcessDefId' }
    @{ Pat = '\bpool\.Id\b'; Rep = 'pool.ProcessDefId' }
    @{ Pat = '\bprocess\.Id\b'; Rep = 'process.ProcessDefId' }
    @{ Pat = '\bprocessDef\.Id\b'; Rep = 'processDef.ProcessDefId' }
    @{ Pat = '\bentity\.PackageId, entity\.Id\)'; Rep = 'entity.PackageId, entity.ProcessDefId)' }

    # Transition
    @{ Pat = '\bti\.Id\b'; Rep = 'ti.TransitionInstanceId' }
    @{ Pat = '\btransitionInstance\.Id\b'; Rep = 'transitionInstance.TransitionInstanceId' }

    # Task
    @{ Pat = '\btaskEntity\.Id\b'; Rep = 'taskEntity.TaskId' }
    @{ Pat = '\bcurrentTask\.Id\b'; Rep = 'currentTask.TaskId' }
    @{ Pat = '\bentrustedTask\.Id\b'; Rep = 'entrustedTask.TaskId' }
    @{ Pat = '\bsrcTask\.Id\b'; Rep = 'srcTask.TaskId' }

    # TaskView
    @{ Pat = '\btaskView\.Id\b'; Rep = 'taskView.TaskId' }
    @{ Pat = '\bview\.Id\b'; Rep = 'view.TaskId' }
    @{ Pat = '\brow\.Id\b'; Rep = 'row.TaskId' }

    # Variable / process variable / hook / rule / log / job / form / copy
    @{ Pat = '\bvariable\.Id\b'; Rep = 'variable.VariableId' }
    @{ Pat = '\bvarEntity\.Id\b'; Rep = 'varEntity.VariableId' }
    @{ Pat = '\bve\.Id\b'; Rep = 've.VariableId' }
    @{ Pat = '\bpv\.Id\b'; Rep = 'pv.ProcessVariableId' }
    @{ Pat = '\bprocessVariable\.Id\b'; Rep = 'processVariable.ProcessVariableId' }
    @{ Pat = '\boutbox\.Id\b'; Rep = 'outbox.HookOutboxId' }
    @{ Pat = '\bhookOutbox\.Id\b'; Rep = 'hookOutbox.HookOutboxId' }
    @{ Pat = '\bo\.Id\b'; Rep = 'o.HookOutboxId' }
    @{ Pat = '\bruleSet\.Id\b'; Rep = 'ruleSet.RuleSetId' }
    @{ Pat = '\brs\.Id\b'; Rep = 'rs.RuleSetId' }
    @{ Pat = '\blog\.Id\b'; Rep = 'log.LogId' }
    @{ Pat = '\bjobInfo\.Id\b'; Rep = 'jobInfo.JobInfoId' }
    @{ Pat = '\bform\.Id\b'; Rep = 'form.FormId' }
    @{ Pat = '\bformEntity\.Id\b'; Rep = 'formEntity.FormId' }
    @{ Pat = '\bcopy\.Id\b'; Rep = 'copy.ProcessCopyId' }
    @{ Pat = '\bcopyEntity\.Id\b'; Rep = 'copyEntity.ProcessCopyId' }
    @{ Pat = '\bfieldActivityEdit\.Id\b'; Rep = 'fieldActivityEdit.FieldActivityEditId' }

    # SQLClip lambdas - ProcessEntity (p)
    @{ Pat = '\(\) => p\.Id\b'; Rep = '() => p.ProcessDefId' }
    @{ Pat = '\(\) => p\.Id,'; Rep = '() => p.ProcessDefId,' }
    @{ Pat = '\(\) => p\.Id\)'; Rep = '() => p.ProcessDefId)' }

    # SQLClip - TaskEntity (t) - after transition ti rules
    @{ Pat = '\(\) => t\.Id\b'; Rep = '() => t.TaskId' }
    @{ Pat = '\(\) => t\.Id,'; Rep = '() => t.TaskId,' }
    @{ Pat = '\(\) => t\.Id\)'; Rep = '() => t.TaskId)' }

    # SQLClip - other entities
    @{ Pat = '\(\) => pi\.Id\b'; Rep = '() => pi.ProcessInstanceId' }
    @{ Pat = '\(\) => ai\.Id\b'; Rep = '() => ai.ActivityInstanceId' }
    @{ Pat = '\(\) => ti\.Id\b'; Rep = '() => ti.TransitionInstanceId' }
    @{ Pat = '\(\) => tv\.Id\b'; Rep = '() => tv.TaskId' }
    @{ Pat = '\(\) => v\.Id\b'; Rep = '() => v.VariableId' }
    @{ Pat = '\(\) => pv\.Id\b'; Rep = '() => pv.ProcessVariableId' }
    @{ Pat = '\(\) => rs\.Id\b'; Rep = '() => rs.RuleSetId' }
    @{ Pat = '\(\) => o\.Id\b'; Rep = '() => o.HookOutboxId' }

    # nameof
    @{ Pat = 'nameof\(ProcessEntity\.Id\)'; Rep = 'nameof(ProcessEntity.ProcessDefId)' }
    @{ Pat = 'nameof\(ProcessInstanceEntity\.Id\)'; Rep = 'nameof(ProcessInstanceEntity.ProcessInstanceId)' }
    @{ Pat = 'nameof\(ActivityInstanceEntity\.Id\)'; Rep = 'nameof(ActivityInstanceEntity.ActivityInstanceId)' }
    @{ Pat = 'nameof\(TaskEntity\.Id\)'; Rep = 'nameof(TaskEntity.TaskId)' }
    @{ Pat = 'nameof\(TaskViewEntity\.Id\)'; Rep = 'nameof(TaskViewEntity.TaskId)' }

    # Activity/Process instance compound identifiers
    @{ Pat = '(\w*ActivityInstance)\.Id\b'; Rep = '${1}.ActivityInstanceId' }
    @{ Pat = '(\w*ProcessInstance)\.Id\b'; Rep = '${1}.ProcessInstanceId' }
    @{ Pat = '\brunningNode\.Id\b'; Rep = 'runningNode.ActivityInstanceId' }
    @{ Pat = '\bnewChildInstance\.Id\b'; Rep = 'newChildInstance.ActivityInstanceId' }
    @{ Pat = '\bTaskView\.Id\b'; Rep = 'TaskView.TaskId' }
    @{ Pat = '\btask\.Id\b'; Rep = 'task.TaskId' }
    @{ Pat = '\bcandidate\.Id\b'; Rep = 'candidate.HookOutboxId' }
    @{ Pat = '\bexistProcessVariable\.Id\b'; Rep = 'existProcessVariable.ProcessVariableId' }
    @{ Pat = '\bexist\.Id\b'; Rep = 'exist.RuleSetId' }
    @{ Pat = '\bx\.Id\b'; Rep = 'x.ProcessVariableId' }  # mostly ProcessVariableManager row keys
    @{ Pat = '\(\) => u\.Id,'; Rep = '() => u.HookOutboxId,' }
)

$perFileEntityId = @{
    'ActivityInstanceManager.cs' = 'ActivityInstanceId'
    'TaskManager.cs' = 'TaskId'
    'ProcessInstanceManager.cs' = 'ProcessInstanceId'
    'TransitionInstanceManager.cs' = 'TransitionInstanceId'
    'ProcessVariableManager.cs' = 'ProcessVariableId'
    'VariableManager.cs' = 'VariableId'
    'RuleSetManager.cs' = 'RuleSetId'
    'HookOutboxManager.cs' = 'HookOutboxId'
    'FormManager.cs' = 'FormId'
    'NodeMediator.cs' = 'ActivityInstanceId'
    'NodeMediatorTask.cs' = 'ActivityInstanceId'
    'NodeMediatorRevise.cs' = 'ActivityInstanceId'
    'NodeMediatorSubProcess.cs' = 'ActivityInstanceId'
    'NodeMediatorSubProcessNested.cs' = 'ActivityInstanceId'
    'NodeMediatorInterMessageGo.cs' = 'ActivityInstanceId'
    'NodeMediatorInterTimerGo.cs' = 'ActivityInstanceId'
    'NodeMediatorInterConditionalGo.cs' = 'ActivityInstanceId'
    'WorkflowService.Query.cs' = 'VariableId'
    'RuleConfigDetail.cs' = 'RuleSetId'
    'FormService.cs' = 'FormId'
}

$changed = 0
foreach ($root in $roots) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem -Path $root -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch $excludeDir } | ForEach-Object {
        $text = [IO.File]::ReadAllText($_.FullName)
        $orig = $text
        foreach ($rule in $rules) {
            $text = [regex]::Replace($text, $rule.Pat, $rule.Rep)
        }
        foreach ($kv in $perFileEntityId.GetEnumerator()) {
            if ($_.Name -like "*$($kv.Key)") {
                $prop = $kv.Value
                $text = [regex]::Replace($text, "\bentity\.$prop =", "entity.__TEMP__ =")
                $text = [regex]::Replace($text, "\bentity\.ProcessDefId\b", "entity.$prop")
                $text = [regex]::Replace($text, "\bentity\.__TEMP__ =", "entity.$prop =")
                if ($kv.Key -eq 'WorkflowService.Query.cs') {
                    $text = [regex]::Replace($text, '\bentity\.ProcessDefId\b', 'entity.VariableId')
                    $text = [regex]::Replace($text, '\bexist\.RuleSetId\b', 'exist.RuleSetId')
                }
                if ($kv.Key -eq 'RuleConfigDetail.cs') {
                    $text = [regex]::Replace($text, 'RuleSetId = entity\.ProcessDefId', 'RuleSetId = entity.RuleSetId')
                }
            }
        }
        if ($text -ne $orig) {
            [IO.File]::WriteAllText($_.FullName, $text)
            $changed++
        }
    }
}
Write-Host "Updated $changed files"
