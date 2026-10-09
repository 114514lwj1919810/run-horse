/* AUTO-FILE-DOC
 * 文件职责：SD/TF 卡 SPI 命令层。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#ifndef __BOOT_SD_H
#define __BOOT_SD_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum
{
    BOOT_SD_OK = 0,
    BOOT_SD_ERR_INIT,
    BOOT_SD_ERR_CMD,
    BOOT_SD_ERR_READ,
    BOOT_SD_ERR_TIMEOUT
} boot_sd_status_t;

/*
 * 作用：按 SPI 模式初始化 TF/SD 卡，识别 SDHC/非 SDHC，并切换到高速 SPI。
 * 使用位置：boot_fat32_mount() 挂载文件系统前调用。
 */
boot_sd_status_t boot_sd_init(void);

/*
 * 作用：读取 TF 卡指定 LBA 的 512 字节扇区。
 * 使用位置：boot_fat32.c 读取 MBR、BPB、FAT 表、目录项和文件数据。
 */
boot_sd_status_t boot_sd_read_sector(uint32_t sector_lba, uint8_t *buffer);

#ifdef __cplusplus
}
#endif

#endif /* __BOOT_SD_H */
