/* AUTO-FILE-DOC
 * 文件职责：Boot/App 确认契约。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#include "boot_contract.h"
#include "gd32f4xx.h"
#include <string.h>
#include <stddef.h>
#ifdef BOOT_APP_BASE
static uint32_t trial_start;
static uint8_t trial_checked, trial_running;
#endif

/**
 * @brief Boot/App 确认契约：执行内部处理（对应符号 Boot_Base）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_main.c:main、boot_contract.c:Boot_VectorValid、boot_contract.c:Boot_AppValid。
 * @param slot 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
uint32_t Boot_Base(uint32_t slot) { return slot == 0U ? BOOT_APP_A : BOOT_APP_B; }
/**
 * @brief Boot/App 确认契约：更新（对应符号 Boot_CrcUpdate）。
 * @调用关系 直接调用者：boot_main.c:verify_file、boot_contract.c:Boot_Crc。
 * @局部状态 主要临时变量：p、i；均只在本次调用中有效。
 * @param crc 按值传入；函数内部修改不会回写调用方变量。
 * @param data 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param size 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
uint32_t Boot_CrcUpdate(uint32_t crc, const void *data, uint32_t size) {
    const uint8_t *p = (const uint8_t *)data;
    uint32_t i, j;
    for (i=0; i<size; ++i) {
        crc ^= p[i];
        for (j=0; j<8U; ++j) crc = (crc >> 1) ^ ((crc & 1U) ? 0xEDB88320UL : 0U);
        if ((i & 1023U)==0U) fwdgt_counter_reload();
    }
    return crc;
}
/**
 * @brief Boot/App 确认契约：计算 CRC（对应符号 Boot_Crc）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_main.c:adopt_factory、boot_contract.c:Boot_RecordValid、boot_contract.c:Boot_Save、boot_contract.c:Boot_AppValid。
 * @param data 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param size 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
uint32_t Boot_Crc(const void *data, uint32_t size) { return Boot_CrcUpdate(0xFFFFFFFFUL,data,size)^0xFFFFFFFFUL; }
/**
 * @brief Boot/App 确认契约：校验（对应符号 Boot_VectorValid）。
 * @调用关系 直接调用者：boot_main.c:verify_file、boot_main.c:try_update、boot_main.c:adopt_factory、boot_contract.c:Boot_AppValid。
 * @局部状态 主要临时变量：sp；均只在本次调用中有效。
 * @param image 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param slot 按值传入；函数内部修改不会回写调用方变量。
 * @param size 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
int Boot_VectorValid(const void *image, uint32_t slot, uint32_t size) {
    uint32_t sp, reset, base;
    if (slot>1U || size<8U || size>BOOT_APP_SIZE) return 0;
    memcpy(&sp,image,4U); memcpy(&reset,(const uint8_t *)image+4,4U);
    base=Boot_Base(slot);
    if ((sp&7U)!=0U) return 0;
    if (!((sp>0x20000000UL && sp<=0x20020000UL) || (sp>0x10000000UL && sp<=0x10010000UL))) return 0;
    return (reset&1U) && (reset>=base) && ((reset&~1UL)<base+size);
}
/**
 * @brief Boot/App 确认契约：校验（对应符号 Boot_RecordValid）。
 * @调用关系 直接调用者：boot_contract.c:Boot_Load、boot_contract.c:Boot_Save。
 * @param r 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
int Boot_RecordValid(const BootRecord *r) {
    return r->magic==BOOT_MAGIC && r->format==1U && r->model==BOOT_MODEL &&
        r->active<2U && r->previous<2U && (r->pending<2U || r->pending==BOOT_NONE) &&
        r->phase<=BOOT_TRIAL && ((r->phase==0U)==(r->pending==BOOT_NONE)) &&
        r->size[0]<=BOOT_APP_SIZE && r->size[1]<=BOOT_APP_SIZE &&
        Boot_Crc(r,offsetof(BootRecord,checksum))==r->checksum;
}
/**
 * @brief Boot/App 确认契约：执行内部处理（对应符号 Boot_Load）。
 * @调用关系 直接调用者：boot_main.c:main、boot_contract.c:Boot_Save、boot_contract.c:BootApp_Confirm、boot_contract.c:BootApp_CheckDeadline。
 * @局部状态 主要临时变量：a、va；均只在本次调用中有效。
 * @param r 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
int Boot_Load(BootRecord *r) {
    const BootRecord *a=(const BootRecord *)BOOT_META_A, *b=(const BootRecord *)BOOT_META_B;
    int va=Boot_RecordValid(a), vb=Boot_RecordValid(b);
    if (!va && !vb) return 0;
    if (vb && (!va || (int32_t)(b->sequence-a->sequence)>0)) a=b;
    memcpy(r,a,sizeof(*r)); return 1;
}
/* VE layout: metadata uses sectors 4/7, applications use sectors 5/6. */
/**
 * @brief Boot/App 确认契约：执行内部处理（对应符号 Boot_FlashErase）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_contract.c:Boot_Save。
 * @局部状态 主要临时变量：sector；均只在本次调用中有效。
 * @param address 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
int Boot_FlashErase(uint32_t address) {
    uint32_t sector, primask; fmc_state_enum status;
    if (address == BOOT_META_A) sector = 4U;
    else if (address == BOOT_APP_A) sector = 5U;
    else if (address == BOOT_APP_B) sector = 6U;
    else if (address == BOOT_META_B) sector = 7U;
    else return 0;
    primask=__get_PRIMASK(); __disable_irq(); fwdgt_counter_reload();
    fmc_unlock(); fmc_flag_clear(FMC_FLAG_END|FMC_FLAG_OPERR|FMC_FLAG_WPERR|FMC_FLAG_PGMERR|FMC_FLAG_PGSERR);
    status=fmc_sector_erase(CTL_SN(sector)); fmc_lock(); fwdgt_counter_reload();
    __set_PRIMASK(primask); return status==FMC_READY;
}
/**
 * @brief Boot/App 确认契约：写入（对应符号 Boot_FlashWrite）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_contract.c:Boot_Save。
 * @局部状态 主要临时变量：i；均只在本次调用中有效。
 * @param address 按值传入；函数内部修改不会回写调用方变量。
 * @param data 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param size 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
int Boot_FlashWrite(uint32_t address, const void *data, uint32_t size) {
    uint32_t i, word, n, primask; const uint8_t *p=(const uint8_t *)data;
    fmc_state_enum status=FMC_READY;
    if ((address&3U) || !size || address<BOOT_META_A || address>=BOOT_FLASH_END ||
        size>BOOT_FLASH_END-address) return 0;
    primask=__get_PRIMASK(); __disable_irq(); fmc_unlock();
    fmc_flag_clear(FMC_FLAG_END|FMC_FLAG_OPERR|FMC_FLAG_WPERR|FMC_FLAG_PGMERR|FMC_FLAG_PGSERR);
    for (i=0; i<size; i+=4U) {
        word=0xFFFFFFFFUL; n=size-i; if(n>4U)n=4U; memcpy(&word,p+i,n);
        fwdgt_counter_reload(); status=fmc_word_program(address+i,word);
        if(status!=FMC_READY || *(volatile uint32_t *)(address+i)!=word) { status=FMC_PGSERR; break; }
    }
    fmc_lock(); __DSB(); __ISB(); __set_PRIMASK(primask); return status==FMC_READY;
}
/* Alternate metadata sectors: commit CRC last, verify new copy, never erase the current copy. */
/**
 * @brief Boot/App 确认契约：执行内部处理（对应符号 Boot_Save）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_main.c:adopt_factory、boot_main.c:main、boot_contract.c:BootApp_Confirm。
 * @局部状态 主要临时变量：current、a；均只在本次调用中有效。
 * @param r 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
int Boot_Save(BootRecord *r) {
    BootRecord current; uint32_t target=BOOT_META_A;
    const BootRecord *a=(const BootRecord *)BOOT_META_A;
    if (Boot_Load(&current)) {
        target=(Boot_RecordValid(a) && a->sequence==current.sequence)?BOOT_META_B:BOOT_META_A;
        r->sequence=current.sequence+1U;
    } else r->sequence=1U;
    r->magic=BOOT_MAGIC; r->format=1U; r->model=BOOT_MODEL;
    r->checksum=Boot_Crc(r,offsetof(BootRecord,checksum));
    if(!Boot_FlashErase(target) || !Boot_FlashWrite(target,r,sizeof(*r))) return 0;
    return Boot_RecordValid((const BootRecord *)target) && memcmp((const void *)target,r,sizeof(*r))==0;
}
/**
 * @brief Boot/App 确认契约：校验（对应符号 Boot_AppValid）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_main.c:main。
 * @param r 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param slot 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
int Boot_AppValid(const BootRecord *r, uint32_t slot) {
    if(slot>1U || (r->phase==BOOT_WRITING && r->pending==slot)) return 0;
    if(!Boot_VectorValid((const void *)Boot_Base(slot),slot,r->size[slot])) return 0;
    return Boot_Crc((const void *)Boot_Base(slot),r->size[slot])==r->crc[slot];
}
/* Called once at the POST completion gate, before gameplay/output restoration. */
/**
 * @brief Boot/App 确认契约：执行内部处理（对应符号 BootApp_Confirm）。
 * @调用关系 直接调用者：未发现普通 C 直接调用；可能由启动文件、中断向量、回调表或外部模块入口调用。
 * @局部状态 主要临时变量：r；均只在本次调用中有效。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void BootApp_Confirm(void) {
#ifdef BOOT_APP_BASE
    BootRecord r; uint32_t slot=(BOOT_APP_BASE==BOOT_APP_A)?0U:1U;
    if(!Boot_Load(&r) || r.active!=slot) { NVIC_SystemReset(); return; }
    if(r.phase==BOOT_TRIAL && r.pending==slot) {
        r.pending=BOOT_NONE; r.phase=0U; r.attempts=0U;
        if(!Boot_Save(&r)) NVIC_SystemReset();
    }
    trial_running=0U; trial_checked=1U;
#endif
}
/**
 * @brief Boot/App 确认契约：检查（对应符号 BootApp_CheckDeadline）。
 * @调用关系 直接调用者：未发现普通 C 直接调用；可能由启动文件、中断向量、回调表或外部模块入口调用。
 * @局部状态 主要临时变量：r；均只在本次调用中有效。
 * @param now_ms 按值传入；函数内部修改不会回写调用方变量。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void BootApp_CheckDeadline(uint32_t now_ms) {
#ifdef BOOT_APP_BASE
    BootRecord r;
    if(!trial_checked) {
        trial_checked=1U;trial_start=now_ms;
        trial_running=(Boot_Load(&r) && r.phase==BOOT_TRIAL)?1U:0U;
    }
    /* Self-test must finish within 60s even if the foreground loop keeps feeding WDG. */
    if(trial_running && now_ms-trial_start>=60000UL) NVIC_SystemReset();
#else
    (void)now_ms;
#endif
}
