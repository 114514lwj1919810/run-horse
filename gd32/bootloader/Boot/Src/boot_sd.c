/* AUTO-FILE-DOC
 * 文件职责：SD/TF 卡 SPI 命令层。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#include "boot_sd.h"
#include "boot_spi1.h"
#include "boot_config.h"
#include "boot_watchdog.h"

#define BOOT_SD_CMD0                  0U
#define BOOT_SD_CMD8                  8U
#define BOOT_SD_CMD17                 17U
#define BOOT_SD_CMD55                 55U
#define BOOT_SD_CMD58                 58U
#define BOOT_SD_ACMD41                41U

#define BOOT_SD_TOKEN_START_BLOCK     0xFEU

static uint8_t s_sd_is_sdhc = 0U;

/*
 * 作用：等待 TF 卡在 SPI 总线上返回 0xFF，表示卡已经不忙。
 * 使用位置：boot_sd_send_cmd() 发送每条命令前调用，避免卡仍在内部处理时继续发命令。
 */
/**
 * @brief SD/TF 卡 SPI 命令层：读取（对应符号 boot_sd_wait_ready）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_send_cmd。
 * @局部状态 主要临时变量：response、start；均只在本次调用中有效。
 * @param timeout 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static uint8_t boot_sd_wait_ready(uint32_t timeout)
{
    uint8_t response;
    uint32_t start = boot_now();

    do
    {
        response = boot_spi1_transfer(0xFFU);
        if (response == 0xFFU)
        {
            return 1U;
        }
        timeout--;
        boot_watchdog_refresh();
    } while ((timeout != 0U) && (boot_now() - start < 100U));

    return 0U;
}

/*
 * 作用：向 TF 卡发送一条 SPI 模式 SD 命令，并读取 R1 响应。
 * 使用位置：boot_sd_init() 和 boot_sd_read_sector() 中发送 CMD0/CMD8/ACMD41/CMD17 等命令。
 */
/**
 * @brief SD/TF 卡 SPI 命令层：编码并发送（对应符号 boot_sd_send_cmd）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_init、boot_sd.c:boot_sd_read_sector。
 * @局部状态 主要临时变量：response、i；均只在本次调用中有效。
 * @param cmd 按值传入；函数内部修改不会回写调用方变量。
 * @param arg 按值传入；函数内部修改不会回写调用方变量。
 * @param crc 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static uint8_t boot_sd_send_cmd(uint8_t cmd, uint32_t arg, uint8_t crc)
{
    uint8_t response;
    uint8_t i;

    boot_tf_select();

    if (boot_sd_wait_ready(50000U) == 0U)
    {
        boot_tf_unselect();
        return 0xFFU;
    }

    (void)boot_spi1_transfer((uint8_t)(0x40U | cmd));
    (void)boot_spi1_transfer((uint8_t)(arg >> 24U));
    (void)boot_spi1_transfer((uint8_t)(arg >> 16U));
    (void)boot_spi1_transfer((uint8_t)(arg >> 8U));
    (void)boot_spi1_transfer((uint8_t)arg);
    (void)boot_spi1_transfer(crc);

    for (i = 0U; i < 10U; i++)
    {
        response = boot_spi1_transfer(0xFFU);
        if ((response & 0x80U) == 0U)
        {
            return response;
        }
        boot_watchdog_refresh();
    }

    return 0xFFU;
}

/*
 * 作用：结束当前 SD 命令事务，释放 TF 片选。
 * 使用位置：boot_sd_init() 和 boot_sd_read_sector() 每条命令完成后调用。
 */
/**
 * @brief SD/TF 卡 SPI 命令层：执行内部处理（对应符号 boot_sd_end_cmd）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_init、boot_sd.c:boot_sd_read_sector。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static void boot_sd_end_cmd(void)
{
    boot_tf_unselect();
}

/*
 * 作用：初始化 TF 卡进入 SPI 模式，并识别是否为 SDHC/SDXC 块寻址卡。
 * 使用位置：boot_fat32_mount() 挂载 FAT32 前调用，是 TF 卡 IAP 的底层入口。
 */
/**
 * @brief SD/TF 卡 SPI 命令层：初始化（对应符号 boot_sd_init）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_mount。
 * @局部状态 主要临时变量：i、response、ocr、timeout、start；均只在本次调用中有效。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
boot_sd_status_t boot_sd_init(void)
{
    uint8_t i;
    uint8_t response;
    uint8_t ocr[4];
    uint32_t timeout;
    uint32_t start;

    s_sd_is_sdhc = 0U;

    boot_spi1_gpio_init();
    boot_spi1_init_slow();
    boot_spi1_shared_bus_idle();

    for (i = 0U; i < 10U; i++)
    {
        (void)boot_spi1_transfer(0xFFU);
    }

    timeout = 200U;
    start = boot_now();
    do
    {
        response = boot_sd_send_cmd(BOOT_SD_CMD0, 0U, 0x95U);
        boot_sd_end_cmd();
        if (response == 0x01U)
        {
            break;
        }
        HAL_Delay(1U);
        boot_watchdog_refresh();
        timeout--;
    } while ((timeout != 0U) && (boot_now() - start < 400U));

    if (response != 0x01U)
    {
        return BOOT_SD_ERR_INIT;
    }

    response = boot_sd_send_cmd(BOOT_SD_CMD8, 0x000001AAUL, 0x87U);
    if (response == 0x01U)
    {
        ocr[0] = boot_spi1_transfer(0xFFU);
        ocr[1] = boot_spi1_transfer(0xFFU);
        ocr[2] = boot_spi1_transfer(0xFFU);
        ocr[3] = boot_spi1_transfer(0xFFU);
        boot_sd_end_cmd();

        if ((ocr[2] != 0x01U) || (ocr[3] != 0xAAU))
        {
            return BOOT_SD_ERR_INIT;
        }

        timeout = 2000U;
        start = boot_now();
        do
        {
            response = boot_sd_send_cmd(BOOT_SD_CMD55, 0U, 0x01U);
            boot_sd_end_cmd();
            if (response > 0x01U)
            {
                return BOOT_SD_ERR_CMD;
            }

            response = boot_sd_send_cmd(BOOT_SD_ACMD41, 0x40000000UL, 0x01U);
            boot_sd_end_cmd();
            if (response == 0x00U)
            {
                break;
            }

            HAL_Delay(1U);
            boot_watchdog_refresh();
            timeout--;
        } while ((timeout != 0U) && (boot_now() - start < 2500U));

        if (response != 0x00U)
        {
            return BOOT_SD_ERR_TIMEOUT;
        }

        response = boot_sd_send_cmd(BOOT_SD_CMD58, 0U, 0x01U);
        if (response != 0x00U)
        {
            boot_sd_end_cmd();
            return BOOT_SD_ERR_CMD;
        }

        ocr[0] = boot_spi1_transfer(0xFFU);
        ocr[1] = boot_spi1_transfer(0xFFU);
        ocr[2] = boot_spi1_transfer(0xFFU);
        ocr[3] = boot_spi1_transfer(0xFFU);
        boot_sd_end_cmd();

        if ((ocr[0] & 0x40U) != 0U)
        {
            s_sd_is_sdhc = 1U;
        }
    }
    else
    {
        boot_sd_end_cmd();
        return BOOT_SD_ERR_INIT;
    }

    /* This implementation requires SDHC/SDXC block addressing, not legacy SDSC. */
    if (s_sd_is_sdhc == 0U) return BOOT_SD_ERR_INIT;
    boot_spi1_init_fast();
    return BOOT_SD_OK;
}

/*
 * 作用：按 LBA 读取 TF 卡 1 个 512 字节逻辑扇区。
 * 使用位置：boot_fat32_read_sector() 读取 MBR、BPB、FAT 表、目录项、APP.BIN 数据时调用。
 */
/**
 * @brief SD/TF 卡 SPI 命令层：读取（对应符号 boot_sd_read_sector）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_read_sector。
 * @局部状态 主要临时变量：address、timeout、token、response、start；均只在本次调用中有效。
 * @param sector_lba 按值传入；函数内部修改不会回写调用方变量。
 * @param buffer 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
boot_sd_status_t boot_sd_read_sector(uint32_t sector_lba, uint8_t *buffer)
{
    uint32_t address;
    uint32_t timeout;
    uint8_t token;
    uint8_t response;
    uint32_t start;

    if (buffer == 0)
    {
        return BOOT_SD_ERR_READ;
    }

    address = (s_sd_is_sdhc != 0U) ? sector_lba : (sector_lba * BOOT_TF_SECTOR_SIZE);

    response = boot_sd_send_cmd(BOOT_SD_CMD17, address, 0x01U);
    if (response != 0x00U)
    {
        boot_sd_end_cmd();
        return BOOT_SD_ERR_CMD;
    }

    timeout = 200000U;
    start = boot_now();
    do
    {
        token = boot_spi1_transfer(0xFFU);
        if (token == BOOT_SD_TOKEN_START_BLOCK)
        {
            break;
        }
        timeout--;
        boot_watchdog_refresh();
    } while ((timeout != 0U) && (boot_now() - start < 300U));

    if (token != BOOT_SD_TOKEN_START_BLOCK)
    {
        boot_sd_end_cmd();
        return BOOT_SD_ERR_TIMEOUT;
    }

    boot_spi1_transfer_buffer(0, buffer, BOOT_TF_SECTOR_SIZE);

    /* 丢弃 2 字节 CRC。SPI 模式下第一版不使用卡返回的 CRC。 */
    (void)boot_spi1_transfer(0xFFU);
    (void)boot_spi1_transfer(0xFFU);

    boot_sd_end_cmd();
    return BOOT_SD_OK;
}
