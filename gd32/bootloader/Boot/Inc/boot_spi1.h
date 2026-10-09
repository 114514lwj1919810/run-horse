/* AUTO-FILE-DOC
 * 文件职责：Bootloader SPI 驱动。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#ifndef __BOOT_SPI1_H
#define __BOOT_SPI1_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/*
 * 作用：初始化 PA4~PA7 的 TF/SPI1 引脚，并让 nRF24L01 退出共享 SPI 总线。
 * 使用位置：boot_board_init() 和 boot_sd_init() 中调用。
 */
void boot_spi1_gpio_init(void);

/*
 * 作用：把 SPI1 配成 TF 卡初始化阶段的低速模式。
 * 使用位置：boot_sd_init() 发送 CMD0/CMD8/ACMD41 前调用。
 */
void boot_spi1_init_slow(void);

/*
 * 作用：TF 卡初始化成功后，把 SPI1 切到较高速模式读取扇区。
 * 使用位置：boot_sd_init() 末尾调用。
 */
void boot_spi1_init_fast(void);

/*
 * 作用：通过 SPI1 发送 1 字节并同时接收 1 字节。
 * 使用位置：boot_sd.c 的 SD 命令、响应、数据读取过程。
 */
uint8_t boot_spi1_transfer(uint8_t data);

/*
 * 作用：通过 SPI1 批量传输数据；tx 可为空，表示连续发送 0xFF 来读数据。
 * 使用位置：boot_sd_read_sector() 读取 512 字节扇区数据。
 */
void boot_spi1_transfer_buffer(const uint8_t *tx, uint8_t *rx, uint32_t length);

/* 作用：拉低 TF_CS 选中 TF 卡。使用位置：boot_sd_send_cmd()。 */
void boot_tf_select(void);

/* 作用：拉高 TF_CS 取消选中 TF 卡，并补 1 个空时钟。使用位置：每条 SD 命令结束。 */
void boot_tf_unselect(void);

/* 作用：让 TF 和 nRF24L01 都不被片选，SPI1 总线进入空闲。使用位置：SPI1 GPIO 初始化后。 */
void boot_spi1_shared_bus_idle(void);

#ifdef __cplusplus
}
#endif

#endif /* __BOOT_SPI1_H */
