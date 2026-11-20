# Unity 自动化回归与一次性 CI runner

统一契约位于各仓 `ci/unity-regression.json`；本目录是共享执行器的唯一维护来源。其他模块在工作流中按完整 Demo 提交 SHA 检出本目录，不改变模块自己的 Core 运行时依赖。

## 执行范围

| 仓库 | 实际执行 |
|---|---|
| Demo | Core EditMode 至少 66 项；构建本地 Bundle；Editor 冒烟至少 28 项；关闭 Domain Reload 两个完整播放周期各至少 35 项；三个旧 Prefab 实际导入；Windows x64 Mono Development Player 构建、冒烟至少 27 项、生命周期至少 35 项 |
| Core main | 临时 Unity 宿主以绝对 `file:` 路径加载当前检出的包，运行预览版至少 104 项 EditMode；与 Demo 稳定包 66 项分开 |
| GGResource | EditMode 至少 15 项；新增 PlayMode 至少 2 项，验证最后持有释放后的原生 TextAsset 销毁，以及显式独占组清理后的缓存和原生对象释放 |
| GGNetwork | EditMode 至少 162 项，含未安装 BestHTTP 的明确失败 / 诊断关闭 |
| GGUI | 现有 EditMode 至少 11 项，不宣称覆盖全部页面栈或 UGUI 后端 |
| GGUser | EditMode 至少 71 项、PlayMode 至少 11 项；仍为本地假渠道 / 网关 |

阈值用于防止空跑和整批用例丢失，新用例可增加数量。报告检查同时核对每个 case 的结果，拒绝任何 Failed / Skipped / Inconclusive。Demo 还必须包含 UI 归属、资源与池销毁、事件退订、迟到结果隔离、监听关闭、重开以及“两次实际退出且设置恢复”的证据，不能只统计 PASS 行。

## 本地复现

Windows、PowerShell 7.2+、Python 3.10+、Git、已为当前用户激活的 Unity **6000.3.13f1**，Demo 还需 Windows Mono 构建支持和匹配的 ToLua 插件。生命周期 Editor 使用图形设备；Player 以 `-batchmode -nographics` 验证逻辑。

从 Demo 仓库根运行：

```powershell
./tools/ci/Invoke-UnityRegression.ps1 -RepositoryRoot $PWD `
  -OutputRoot 'D:/ggframework-results/demo-new-run' `
  -PythonExecutable 'C:/Python312/python.exe'
```

其他模块把 `-RepositoryRoot` 改为对应仓库根，使用其工作流固定的共享脚本版本。输出目录必须不存在；旧项目报告和旧构建入口文件会先移动到本轮私有目录，不覆盖历史证据。所有进程有超时，退出码非零、报告未新生成、缺少任一阶段或断言都会失败。Core 临时项目位于输出目录，必须放在 Core 包目录外。

## CI 触发与 runner 启动

`PR CI Contract` 在 GitHub 托管 Ubuntu runner 上检查契约、证据门禁反向测试和 PowerShell 语法，不执行 Unity。`Unity Regression` 在分支 push 或手动触发时先检查契约，再为**仓库拥有者触发的受信任提交**排队一个实际 Unity job；fork PR 不会直接使用工作站。其他作者提交由拥有者审查后在可信分支推送或手动触发。不要把静态契约通过当成 Unity 已运行。

当前采用一次性 Windows runner：每个 workflow run / attempt 使用独立标签，平时没有常驻 runner。未启动时 job 会保持排队，不能计为通过。在已经审查的本地共享脚本上执行：

```powershell
./tools/ci/Start-OneShotRunner.ps1 `
  -Repository gonglei007/GGFramework-Demo `
  -Commit '<本次 push 的完整 40 位 SHA>' `
  -RunnerRoot 'D:/ggframework-runner' `
  -PythonExecutable 'C:/Python312/python.exe'
```

启动器要求 `gh` 已以仓库拥有者登录，精确核对仓库、SHA、触发者、事件、工作流路径、run attempt 和排队标签。只使用官方下载且 SHA-256 校验通过的 runner；只运行一个 job，不安装 Windows 服务，不复制或上传本机 Unity 许可证。结束后注销 runner，即使失败也执行清理。runner 包、工作目录可保留供排查，不等于服务或注册仍存在。

首次引入工作流会由分支 push 启动。合并后 main / master 的新 SHA 会启动另一轮，需要再次提供一次性 runner；重跑失败 workflow 也有新的 attempt 标签。`-RunId` 可以明确指定一个已有 run。启动器不会自动合并，合并前仍须核对当前 PR、评论、所有检查和最新头提交。

不要把该启动器改成接收任意 fork 代码的常驻个人工作站 runner。GitHub 关于公共仓库与工作站 runner 的[信任边界](https://docs.github.com/en/actions/reference/security/secure-use#self-hosted-runners)仍适用；持续无人值守运行应另行准备隔离且已授权的专用环境。

## 证据和失败诊断

每次 job 发布 `unity-regression-<SHA>` artifact，保留 14 天。公共 `results.json` 包含准确提交、Unity / OS 环境、阶段时间与退出码、逐项 NUnit 结果、集成断言、原始报告 SHA-256 和构建产物摘要。Step Summary 给出结果；失败发布 `failure.json` 和筛选后的编译 / 断言诊断。完整 Unity 日志及包含机器 / 授权信息的 XML 留在 runner 私有临时目录，**不公开上传**；runner 可能在 job 结束后清理临时目录。

通过以下检查定位结果：

```powershell
gh pr checks <PR号> -R gonglei007/GGFramework-Demo
gh run view <run-id> -R gonglei007/GGFramework-Demo
gh run download <run-id> -R gonglei007/GGFramework-Demo -n unity-regression-<SHA> -D ./ci-evidence
```

本 CI 不验收鼠标 / 键盘逐项操作、视觉、全量 CPU / GPU 内存泄漏、共享同名资产多业务拥有者、所有异步竞态、移动 / IL2CPP / WebGL、真实渠道支付、远程热更新发布。资源 PlayMode 测试不初始化 Loxodon，不把显式独占组清理外推为安全清理其他拥有者。

## 维护契约

新增阶段或必要清理断言时同时改契约与报告检查器，不能为了让失败变绿降低阈值或删除 required evidence。修改共享脚本先在 Demo PR 通过本地与真实 CI，再由各模块的独立 PR 更新固定 SHA；Core 稳定标签及其他运行时依赖保持各自锁定。新工作流名称可供分支保护配置，但本次不改各源仓原有保护策略；知识库原有 17 项必需检查保持不变。
