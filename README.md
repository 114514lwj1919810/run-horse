# GD32 Animal Race

基于 GD32F407VET6 与 Android Unity 的赛马游戏系统。GD32 负责按键、投注、返奖控制、EEPROM 参数和业务状态机，Unity 负责画面、动画、设置界面及串口状态展示。

## 目录

- `gd32/application`：GD32F407VET6 主程序、V3.x 通信协议和 Keil 工程。
- `gd32/bootloader`：支持 TF 卡升级的安全 Bootloader。
- `gd32/tf-update-tools`：固件打包、TF 卡目录生成和工厂镜像脚本。
- `unity`：Unity Android/PC 工程源码、场景、资源、串口插件和编辑器测试。

## 开发环境

- MCU：GD32F407VET6
- IDE：Keil MDK 5
- ARM Compiler：6.24
- Unity：2018.4.36f1
- Android：ARMv7，最低 Android API 19
- MCU 与 Android：板载串口 3（GD32 外设名 `USART2`）

## 构建

### GD32 应用

使用 Keil 打开：

```text
gd32/application/MDK-ARM/Project_IAP_A.uvprojx
```

应用与 Bootloader 配套时应使用 IAP 分区工程，不要把普通 `Project.uvprojx` 直接烧到应用分区。

### Bootloader

使用 Keil 打开：

```text
gd32/bootloader/Projects/MDK-ARM/GD_BOOT_SECURE.uvprojx
```

TF 卡升级包和首次烧录镜像的生成方法见 `gd32/tf-update-tools/README.md`。

### Unity

用 Unity 2018.4.36f1 打开 `unity` 目录。主要构建入口位于：

```text
unity/Assets/Editor/RK3128Build.cs
```

该脚本中的 Android SDK、JDK 和输出目录是开发机路径，换电脑后需要改成本机路径。公开仓库不包含签名证书和签名口令，请在 Unity Player Settings 中配置自己的证书。

## 安全说明

公开版本已将设备维护密码默认值改为 `0`，仅作为占位值。正式生产前必须同时修改 GD32 与 Unity 的默认维护密码，并通过 EEPROM 初始化或生产配置写入实际参数。私钥、Keystore、APK、固件二进制和构建缓存不会提交到仓库。

## 协议与职责

- GD32 是投注、胜者、返奖和积分账目的唯一权威端。
- Unity 只根据 MCU 指定状态播放动画并显示数据。
- 通信包含序号、局号、ACK、重复包去重、全量状态快照和超时恢复。
- Unity 设置项与 GD32 EEPROM 参数通过设置快照及命令协议同步。
