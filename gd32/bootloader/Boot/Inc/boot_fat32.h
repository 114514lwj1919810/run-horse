/* AUTO-FILE-DOC
 * 文件职责：FAT32 只读解析。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#ifndef __BOOT_FAT32_H
#define __BOOT_FAT32_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum
{
    BOOT_FAT32_OK = 0,
    BOOT_FAT32_ERR_MOUNT,
    BOOT_FAT32_ERR_NOT_FOUND,
    BOOT_FAT32_ERR_READ,
    BOOT_FAT32_ERR_UNSUPPORTED
} boot_fat32_status_t;

typedef struct
{
    uint32_t first_cluster;
    uint32_t current_cluster;
    uint32_t current_sector_in_cluster;
    uint32_t size;
    uint32_t position;
} boot_fat32_file_t;

/*
 * 作用：初始化 TF 卡并解析 MBR/BPB，建立 FAT32 卷参数。
 * 使用位置：boot_source_tf.c 打开升级文件前调用。
 */
boot_fat32_status_t boot_fat32_mount(void);

/*
 * 作用：在根目录下查找指定目录，再在该目录中查找指定文件。
 * 使用位置：boot_source_tf.c 打开 /IAP/APP.BIN 和 /IAP/APP.INF。
 */
boot_fat32_status_t boot_fat32_open_path(const char *dir_name,
                                          const char *file_name,
                                          boot_fat32_file_t *file);

/*
 * 作用：从已打开的 FAT32 文件中顺序读取数据。
 * 使用位置：boot_source_tf.c 分块读取 APP.BIN 和读取 APP.INF。
 */
boot_fat32_status_t boot_fat32_read(boot_fat32_file_t *file,
                                     uint8_t *buffer,
                                     uint32_t buffer_size,
                                     uint32_t *read_size);

#ifdef __cplusplus
}
#endif

#endif /* __BOOT_FAT32_H */
