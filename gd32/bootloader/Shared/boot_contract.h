/* AUTO-FILE-DOC
 * 文件职责：Boot/App 确认契约。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#ifndef BOOT_CONTRACT_H
#define BOOT_CONTRACT_H
#include <stdint.h>
#define BOOT_APP_A 0x08020000UL
#define BOOT_APP_B 0x08040000UL
#define BOOT_APP_SIZE 0x00020000UL
#define BOOT_META_A 0x08010000UL
#define BOOT_META_B 0x08060000UL
#define BOOT_FLASH_END 0x08080000UL
#define BOOT_MODEL 0x31305241UL /* A R 0 1: AnimalRace v1 */
#define BOOT_MAGIC 0x47444231UL
#define BOOT_PACKAGE_MAGIC 0x47445031UL
#define BOOT_NONE 0xFFFFFFFFUL
#define BOOT_WRITING 1UL
#define BOOT_TRIAL 2UL
#define BOOT_MAX_ATTEMPTS 2UL
/* 持久化状态记录。sequence 用于双副本选新；active/previous/pending 描述切换；
 * size/crc/release 分别绑定两个 App；checksum 覆盖前面的全部字段。 */
typedef struct {
    uint32_t magic, format, sequence, active, previous, pending, attempts, phase;
    uint32_t size[2], crc[2], release[2];
    uint32_t rejected_release, model, reserved[3], checksum;
} BootRecord;
/* TF 卡 UPDATE.INF 的32字节小端清单；Bootloader 先核对型号、长度和CRC再擦写。 */
typedef struct {
    uint32_t magic, model, release, size_a, crc_a, size_b, crc_b, checksum;
} BootPackage;
uint32_t Boot_CrcUpdate(uint32_t crc, const void *data, uint32_t size);
uint32_t Boot_Crc(const void *data, uint32_t size);
uint32_t Boot_Base(uint32_t slot);
int Boot_VectorValid(const void *image, uint32_t slot, uint32_t size);
int Boot_RecordValid(const BootRecord *r);
int Boot_Load(BootRecord *r);
int Boot_Save(BootRecord *r);
int Boot_AppValid(const BootRecord *r, uint32_t slot);
int Boot_FlashErase(uint32_t address);
int Boot_FlashWrite(uint32_t address, const void *data, uint32_t size);
void BootApp_Confirm(void);
void BootApp_CheckDeadline(uint32_t now_ms);
#endif
