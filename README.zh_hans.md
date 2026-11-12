# Hosihikari.Binding

[English](README.md) | [简体中文](README.zh_hans.md)

星光投影 Interop 面之上的手写 .NET Idiomatic 层。

`Hosihikari.Binding` 是共享基座；`Hosihikari.Binding.{Bedrock,Endstone,LeviLamina}` 各自架在对应的 Interop 程序集之上。Idiomatic 类型包装 Interop 包装，公开属性、事件、运算符、索引器与构造函数；没有搬运过来的成员经 `.Interop` 拿回原包装。

## 边界

- **只面向插件作者。** 框架消费 `.Interop` 程序集，从不消费本层，因此这里的破坏性变更不牵动框架的兼容轴。
- **组合，不继承。** 生成面不会渗进 Idiomatic 的签名；生成器改名输出也不改变本层的二进制 ABI。
- **不生成。** 这里全部是手写代码。覆盖范围是 `docs/` 下每面一份的显式清单；没有列出的类型与成员留在 Interop。
- **与 Interop 成对锁步。** 两者携带同一上游版本与同一绑定修订号，包依赖精确的 Interop 区间。

## 拥有与借用

Idiomatic 镜像拥有权。只能借用到达的类型不实现 `IDisposable`；能被拥有的实现它。两条路都有的类型总是实现 `IDisposable`，并且释放一个借用实例只让该实例失效——对它调用 `ReleaseHandle` 会抛异常。

`From(interop)` 是桥。它按（原生目标，代际）只保留一个实例；目标已被桥接时吸收传入的包装：拥有句柄转交给既有实例，借用的直接丢弃。

## 作用域与线程

每个订阅都登记在 `BindingScope` 上并随它释放，因为挂在共享原生对象上的处理器会钉住加载上下文。没有作用域时订阅会抛异常。默认取调用程序集的作用域；显式传入可以更早收回。

本层不替你切线程。游戏对象的线程亲和写在每个成员的文档里，Debug 构建会在入口断言；跨线程调用走服务端调度器。

## 清单

`docs/idiomatic-<surface>.tsv` 每个类型一行：类型在单数根下的全名、获得的形态、以及刻意保留为方法并给出理由的成员。`scripts/verify_manifests.py` 会把每一行解析到仓库里已提交的 Interop 源。

## 现状

共享基座包含身份表、作用域、事件分发、错误通道，以及服务端 SynchronizationContext 与配套的 TaskScheduler。各面覆盖：Bedrock 的值类型（向量、方块坐标与区块坐标、浮点盒与方块对齐盒两对）；Endstone 的玩家/服务器/物品/世界/命令/插件类型，含托管事件注册与 ActionForm；LeviLamina 的服务、命令、事件、Hook 与服务端线程执行器入口。

三个运行时冒烟在真实服务器里加载本层：

- `scripts/run-runtime-smoke.ps1 -ServerRoot <bds>` —— LeviLamina 载体。
- `scripts/run-runtime-smoke-endstone.ps1 -ServerRoot <endstone bds>` —— Endstone 载体。根目录需部署框架的 Endstone 宿主（`HostingShim/carriers/endstone/deploy.bat`），并且 `-Python` 指向的 venv 里装有 `endstone` CLI。
- `scripts/run-runtime-smoke-bedrock.ps1 -ServerRoot <bds> [-BedrockRoot <checkout>]` —— Bedrock 面。
