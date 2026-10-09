/* AUTO-FILE-DOC
 * 文件职责：FAT32 只读解析。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#include "boot_fat32.h"
#include "boot_sd.h"
#include "boot_config.h"
#include "boot_watchdog.h"

#include <string.h>

#define BOOT_FAT32_ATTR_DIRECTORY       0x10U
#define BOOT_FAT32_ATTR_LONG_NAME       0x0FU
#define BOOT_FAT32_EOC                  0x0FFFFFF8UL

typedef struct
{
    uint32_t partition_lba;
    uint16_t bytes_per_sector;
    uint8_t sectors_per_cluster;
    uint16_t reserved_sector_count;
    uint8_t fat_count;
    uint32_t fat_size;
    uint32_t fat_begin_lba;
    uint32_t data_begin_lba;
    uint32_t root_cluster;
    uint32_t cluster_count;
} boot_fat32_volume_t;

static boot_fat32_volume_t s_fat32;
static uint8_t s_sector[BOOT_TF_SECTOR_SIZE];

/*
 * 作用：从 FAT32 扇区数据中读取 16 位小端数。
 * 使用位置：挂载 BPB、解析目录项时读取 FAT32 字段。
 */
/**
 * @brief FAT32 只读解析：执行内部处理（对应符号 boot_rd16）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_find_in_dir、boot_fat32.c:boot_fat32_mount。
 * @param p 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static uint16_t boot_rd16(const uint8_t *p)
{
    return (uint16_t)p[0] | ((uint16_t)p[1] << 8U);
}

/*
 * 作用：从 FAT32 扇区数据中读取 32 位小端数。
 * 使用位置：挂载 BPB、解析目录项、读取 FAT 表项时读取 FAT32 字段。
 */
/**
 * @brief FAT32 只读解析：执行内部处理（对应符号 boot_rd32）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_next_cluster、boot_fat32.c:boot_fat32_find_in_dir、boot_fat32.c:boot_fat32_mount。
 * @param p 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static uint32_t boot_rd32(const uint8_t *p)
{
    return (uint32_t)p[0] |
           ((uint32_t)p[1] << 8U) |
           ((uint32_t)p[2] << 16U) |
           ((uint32_t)p[3] << 24U);
}

/*
 * 作用：把 FAT32 簇号转换成 TF 卡 LBA 扇区号。
 * 使用位置：目录遍历和文件读取时定位当前簇对应的数据区扇区。
 */
/**
 * @brief FAT32 只读解析：执行内部处理（对应符号 boot_fat32_cluster_to_lba）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_find_in_dir、boot_fat32.c:boot_fat32_read。
 * @param cluster 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static uint32_t boot_fat32_cluster_to_lba(uint32_t cluster)
{
    return s_fat32.data_begin_lba +
           ((cluster - 2U) * (uint32_t)s_fat32.sectors_per_cluster);
}

/*
 * 作用：读取 TF 卡 1 个扇区，并把 SD 层错误转换成 FAT32 层错误。
 * 使用位置：FAT32 挂载、FAT 表读取、目录读取和文件读取的统一底层读扇区入口。
 */
/**
 * @brief FAT32 只读解析：读取（对应符号 boot_fat32_read_sector）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_next_cluster、boot_fat32.c:boot_fat32_find_in_dir、boot_fat32.c:boot_fat32_mount、boot_fat32.c:boot_fat32_read。
 * @param lba 按值传入；函数内部修改不会回写调用方变量。
 * @param buffer 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static boot_fat32_status_t boot_fat32_read_sector(uint32_t lba, uint8_t *buffer)
{
    if (boot_sd_read_sector(lba, buffer) != BOOT_SD_OK)
    {
        return BOOT_FAT32_ERR_READ;
    }

    return BOOT_FAT32_OK;
}

/*
 * 作用：从 FAT 表中读取当前簇的下一个簇号。
 * 使用位置：目录跨簇遍历和 APP.BIN 顺序读取跨簇时调用。
 */
/**
 * @brief FAT32 只读解析：执行内部处理（对应符号 boot_fat32_next_cluster）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_find_in_dir、boot_fat32.c:boot_fat32_read。
 * @局部状态 主要临时变量：fat_offset、fat_sector、entry_offset、next；均只在本次调用中有效。
 * @param cluster 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static uint32_t boot_fat32_next_cluster(uint32_t cluster)
{
    uint32_t fat_offset;
    uint32_t fat_sector;
    uint32_t entry_offset;
    uint32_t next;

    if (cluster < 2U || cluster - 2U >= s_fat32.cluster_count) return 0xFFFFFFFFUL;
    fat_offset = cluster * 4U;
    fat_sector = s_fat32.fat_begin_lba + (fat_offset / BOOT_TF_SECTOR_SIZE);
    entry_offset = fat_offset % BOOT_TF_SECTOR_SIZE;

    if (boot_fat32_read_sector(fat_sector, s_sector) != BOOT_FAT32_OK)
    {
        return 0xFFFFFFFFUL;
    }

    next = boot_rd32(&s_sector[entry_offset]) & 0x0FFFFFFFUL;
    return next;
}

/*
 * 作用：把普通字符串文件名转换成 FAT32 8.3 短文件名格式。
 * 使用位置：boot_fat32_find_in_dir() 查找 IAP 目录、APP.BIN、APP.INF 前调用。
 */
/**
 * @brief FAT32 只读解析：执行内部处理（对应符号 boot_fat32_make_83_name）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_find_in_dir。
 * @局部状态 主要临时变量：i、j；均只在本次调用中有效。
 * @param name 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param out 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static void boot_fat32_make_83_name(const char *name, uint8_t out[11])
{
    uint8_t i;
    uint8_t j = 0U;

    for (i = 0U; i < 11U; i++)
    {
        out[i] = ' ';
    }

    for (i = 0U; (name[i] != 0) && (name[i] != '.') && (j < 8U); i++)
    {
        out[j++] = (name[i] >= 'a' && name[i] <= 'z') ?
                   (uint8_t)(name[i] - 32) : (uint8_t)name[i];
    }

    if (name[i] == '.')
    {
        i++;
        j = 8U;
        while ((name[i] != 0) && (j < 11U))
        {
            out[j++] = (name[i] >= 'a' && name[i] <= 'z') ?
                       (uint8_t)(name[i] - 32) : (uint8_t)name[i];
            i++;
        }
    }
}

/*
 * 作用：在指定目录簇链中查找一个目录或文件。
 * 使用位置：boot_fat32_open_path() 先找 /IAP 目录，再找 APP.BIN/APP.INF 文件。
 */
/**
 * @brief FAT32 只读解析：执行内部处理（对应符号 boot_fat32_find_in_dir）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_open_path。
 * @局部状态 主要临时变量：target、cluster、sector_index、entry、dirent、attr、visited；均只在本次调用中有效。
 * @param dir_cluster 按值传入；函数内部修改不会回写调用方变量。
 * @param name 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param want_dir 按值传入；函数内部修改不会回写调用方变量。
 * @param file 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static boot_fat32_status_t boot_fat32_find_in_dir(uint32_t dir_cluster,
                                                   const char *name,
                                                   uint8_t want_dir,
                                                   boot_fat32_file_t *file)
{
    uint8_t target[11];
    uint32_t cluster;
    uint32_t sector_index;
    uint32_t entry;
    const uint8_t *dirent;
    uint8_t attr;
    uint32_t visited = 0U;

    if (file == 0)
    {
        return BOOT_FAT32_ERR_READ;
    }

    boot_fat32_make_83_name(name, target);
    cluster = dir_cluster;

    while ((cluster >= 2U) && (cluster < BOOT_FAT32_EOC))
    {
        if (++visited > 64U || cluster - 2U >= s_fat32.cluster_count) return BOOT_FAT32_ERR_READ;
        for (sector_index = 0U; sector_index < s_fat32.sectors_per_cluster; sector_index++)
        {
            if (boot_fat32_read_sector(boot_fat32_cluster_to_lba(cluster) + sector_index,
                                       s_sector) != BOOT_FAT32_OK)
            {
                return BOOT_FAT32_ERR_READ;
            }

            for (entry = 0U; entry < BOOT_TF_SECTOR_SIZE; entry += 32U)
            {
                dirent = &s_sector[entry];

                if (dirent[0] == 0x00U)
                {
                    return BOOT_FAT32_ERR_NOT_FOUND;
                }

                if (dirent[0] == 0xE5U)
                {
                    continue;
                }

                attr = dirent[11];
                if (attr == BOOT_FAT32_ATTR_LONG_NAME)
                {
                    continue;
                }

                if (memcmp(dirent, target, 11U) != 0)
                {
                    continue;
                }

                if (want_dir != 0U)
                {
                    if ((attr & BOOT_FAT32_ATTR_DIRECTORY) == 0U)
                    {
                        return BOOT_FAT32_ERR_NOT_FOUND;
                    }
                }
                else
                {
                    if ((attr & BOOT_FAT32_ATTR_DIRECTORY) != 0U)
                    {
                        return BOOT_FAT32_ERR_NOT_FOUND;
                    }
                }

                file->first_cluster = ((uint32_t)boot_rd16(&dirent[20]) << 16U) |
                                      (uint32_t)boot_rd16(&dirent[26]);
                file->current_cluster = file->first_cluster;
                file->current_sector_in_cluster = 0U;
                file->size = boot_rd32(&dirent[28]);
                file->position = 0U;
                return BOOT_FAT32_OK;
            }
        }

        cluster = boot_fat32_next_cluster(cluster);
        boot_watchdog_refresh();
    }

    return BOOT_FAT32_ERR_NOT_FOUND;
}

/*
 * 作用：初始化 TF 卡并挂载 FAT32 分区，解析 MBR/BPB/FAT 起始位置/数据区起始位置。
 * 使用位置：boot_tf_open() 准备打开 /IAP/APP.BIN 前调用。
 */
/**
 * @brief FAT32 只读解析：执行内部处理（对应符号 boot_fat32_mount）。
 * @调用关系 直接调用者：boot_main.c:try_update。
 * @局部状态 主要临时变量：boot_lba、signature、partition_type、total、flags；均只在本次调用中有效。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
boot_fat32_status_t boot_fat32_mount(void)
{
    uint32_t boot_lba;
    uint16_t signature;
    uint8_t partition_type;
    uint32_t total, overhead, active_fat;
    uint16_t flags;

    if (boot_sd_init() != BOOT_SD_OK)
    {
        return BOOT_FAT32_ERR_MOUNT;
    }

    if (boot_fat32_read_sector(0U, s_sector) != BOOT_FAT32_OK)
    {
        return BOOT_FAT32_ERR_READ;
    }

    signature = boot_rd16(&s_sector[510]);
    if (signature != 0xAA55U)
    {
        return BOOT_FAT32_ERR_MOUNT;
    }

    partition_type = s_sector[0x1BEU + 4U];
    if ((partition_type == 0x0BU) || (partition_type == 0x0CU))
    {
        boot_lba = boot_rd32(&s_sector[0x1BEU + 8U]);
    }
    else
    {
        boot_lba = 0U;
    }

    if (boot_fat32_read_sector(boot_lba, s_sector) != BOOT_FAT32_OK)
    {
        return BOOT_FAT32_ERR_READ;
    }

    if (boot_rd16(&s_sector[510]) != 0xAA55U)
    {
        return BOOT_FAT32_ERR_MOUNT;
    }

    s_fat32.partition_lba = boot_lba;
    s_fat32.bytes_per_sector = boot_rd16(&s_sector[11]);
    s_fat32.sectors_per_cluster = s_sector[13];
    s_fat32.reserved_sector_count = boot_rd16(&s_sector[14]);
    s_fat32.fat_count = s_sector[16];
    s_fat32.fat_size = boot_rd32(&s_sector[36]);
    s_fat32.root_cluster = boot_rd32(&s_sector[44]);

    if ((s_fat32.bytes_per_sector != BOOT_TF_SECTOR_SIZE) ||
        (s_fat32.sectors_per_cluster == 0U) ||
        ((s_fat32.sectors_per_cluster & (s_fat32.sectors_per_cluster - 1U)) != 0U) ||
        (s_fat32.sectors_per_cluster > 128U) ||
        (boot_rd16(&s_sector[17]) != 0U) ||
        (boot_rd16(&s_sector[22]) != 0U) ||
        (s_fat32.reserved_sector_count == 0U) ||
        (s_fat32.fat_count == 0U) || (s_fat32.fat_count > 2U) ||
        (s_fat32.fat_size == 0U) ||
        (s_fat32.root_cluster < 2U))
    {
        return BOOT_FAT32_ERR_UNSUPPORTED;
    }

    total = boot_rd32(&s_sector[32]);
    if (!total || total > 0xFFFFFFFFUL - boot_lba ||
        s_fat32.fat_size > (total - 1U) / s_fat32.fat_count)
        return BOOT_FAT32_ERR_UNSUPPORTED;
    overhead = s_fat32.fat_count * s_fat32.fat_size;
    if (overhead >= total || s_fat32.reserved_sector_count >= total - overhead)
        return BOOT_FAT32_ERR_UNSUPPORTED;
    overhead += s_fat32.reserved_sector_count;
    s_fat32.cluster_count = (total - overhead) / s_fat32.sectors_per_cluster;
    if (s_fat32.cluster_count < 65525U || s_fat32.cluster_count > 0x0FFFFFEEUL ||
        s_fat32.root_cluster - 2U >= s_fat32.cluster_count ||
        (s_fat32.cluster_count + 2U + 127U) / 128U > s_fat32.fat_size ||
        boot_rd16(&s_sector[42]) != 0U)
        return BOOT_FAT32_ERR_UNSUPPORTED;
    flags = boot_rd16(&s_sector[40]);
    active_fat = (flags & 0x80U) ? (flags & 0x0FU) : 0U;
    if (active_fat >= s_fat32.fat_count) return BOOT_FAT32_ERR_UNSUPPORTED;
    s_fat32.fat_begin_lba = s_fat32.partition_lba +
                            (uint32_t)s_fat32.reserved_sector_count;
    s_fat32.data_begin_lba = s_fat32.fat_begin_lba +
                             ((uint32_t)s_fat32.fat_count * s_fat32.fat_size);
    s_fat32.fat_begin_lba += active_fat * s_fat32.fat_size;

    return BOOT_FAT32_OK;
}

/*
 * 作用：打开根目录下指定目录中的指定短文件名文件。
 * 使用位置：boot_tf_open() 打开 /IAP/APP.BIN 和必需的 /IAP/APP.INF。
 */
/**
 * @brief FAT32 只读解析：打开（对应符号 boot_fat32_open_path）。
 * @调用关系 直接调用者：boot_main.c:open_image、boot_main.c:try_update。
 * @param dir_name 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param file_name 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param file 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
boot_fat32_status_t boot_fat32_open_path(const char *dir_name,
                                          const char *file_name,
                                          boot_fat32_file_t *file)
{
    boot_fat32_status_t status;
    boot_fat32_file_t dir;

    status = boot_fat32_find_in_dir(s_fat32.root_cluster, dir_name, 1U, &dir);
    if (status != BOOT_FAT32_OK)
    {
        return status;
    }

    return boot_fat32_find_in_dir(dir.first_cluster, file_name, 0U, file);
}

/*
 * 作用：从已打开的 FAT32 文件中顺序读取数据。
 * 使用位置：boot_tf_read() 连续读取 APP.BIN，boot_tf_open() 读取 APP.INF。
 */
/**
 * @brief FAT32 只读解析：读取（对应符号 boot_fat32_read）。
 * @调用关系 直接调用者：boot_main.c:verify_file、boot_main.c:try_update。
 * @局部状态 主要临时变量：remaining、sector_lba、copy_size、sector_offset；均只在本次调用中有效。
 * @param file 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @param buffer 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @param buffer_size 按值传入；函数内部修改不会回写调用方变量。
 * @param read_size 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
boot_fat32_status_t boot_fat32_read(boot_fat32_file_t *file,
                                     uint8_t *buffer,
                                     uint32_t buffer_size,
                                     uint32_t *read_size)
{
    uint32_t remaining;
    uint32_t sector_lba;
    uint32_t copy_size;
    uint32_t sector_offset;

    if ((file == 0) || (buffer == 0) || (read_size == 0))
    {
        return BOOT_FAT32_ERR_READ;
    }

    *read_size = 0U;

    if (file->position >= file->size)
    {
        return BOOT_FAT32_OK;
    }

    remaining = file->size - file->position;
    if (remaining > buffer_size)
    {
        remaining = buffer_size;
    }

    while (remaining != 0U)
    {
        if (file->current_cluster < 2U || file->current_cluster - 2U >= s_fat32.cluster_count)
            return BOOT_FAT32_ERR_READ;
        sector_offset = file->position % BOOT_TF_SECTOR_SIZE;
        sector_lba = boot_fat32_cluster_to_lba(file->current_cluster) +
                     file->current_sector_in_cluster;

        if (boot_fat32_read_sector(sector_lba, s_sector) != BOOT_FAT32_OK)
        {
            return BOOT_FAT32_ERR_READ;
        }

        copy_size = remaining;
        if (copy_size > BOOT_TF_SECTOR_SIZE - sector_offset)
        {
            copy_size = BOOT_TF_SECTOR_SIZE - sector_offset;
        }

        memcpy(&buffer[*read_size], &s_sector[sector_offset], copy_size);

        *read_size += copy_size;
        file->position += copy_size;
        remaining -= copy_size;

        if ((file->position % BOOT_TF_SECTOR_SIZE) != 0U) continue;
        file->current_sector_in_cluster++;
        if (file->current_sector_in_cluster >= s_fat32.sectors_per_cluster)
        {
            file->current_sector_in_cluster = 0U;
            file->current_cluster = boot_fat32_next_cluster(file->current_cluster);
            if (file->current_cluster >= BOOT_FAT32_EOC)
            {
                if (file->position < file->size) return BOOT_FAT32_ERR_READ;
                break;
            }
        }

        boot_watchdog_refresh();
    }

    return BOOT_FAT32_OK;
}
