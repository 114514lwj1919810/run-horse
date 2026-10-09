/* AUTO-FILE-DOC
 * 文件职责：Bootloader 时钟与中断基础。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#include "gd32f4xx.h"
/* Boot uses reset IRC16M: no external crystal dependency, SPI init <= 400 kHz. */
uint32_t SystemCoreClock = 16000000U;
/**
 * @brief Bootloader 时钟与中断基础：执行内部处理（对应符号 safe）。
 * @调用关系 直接调用者：boot_system.c:SystemInit。
 * @局部状态 主要临时变量：i；均只在本次调用中有效。
 * @param port 按值传入；函数内部修改不会回写调用方变量。
 * @param pins 按值传入；函数内部修改不会回写调用方变量。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static void safe(uint32_t port,uint32_t pins) {
    uint32_t i, mask=0U, output=0U, pull=0U;
    for(i=0;i<16U;i++) if(pins&(1UL<<i)) {mask|=3UL<<(2U*i);output|=1UL<<(2U*i);pull|=2UL<<(2U*i);}
    GPIO_BC(port)=pins; GPIO_OMODE(port)&=~pins;
    GPIO_PUD(port)=(GPIO_PUD(port)&~mask)|pull;
    GPIO_CTL(port)=(GPIO_CTL(port)&~mask)|output;
}
/**
 * @brief Bootloader 时钟与中断基础：初始化（对应符号 SystemInit）。
 * @调用关系 直接调用者：未发现普通 C 直接调用；可能由启动文件、中断向量、回调表或外部模块入口调用。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void SystemInit(void) {
    RCU_AHB1EN |= RCU_AHB1EN_PBEN|RCU_AHB1EN_PDEN|RCU_AHB1EN_PEEN|RCU_AHB1EN_PGEN;
    (void)RCU_AHB1EN; __DSB();
    safe(GPIOD,BIT(8)|BIT(9)|BIT(10)|BIT(15));safe(GPIOB,BIT(15));safe(GPIOE,BIT(11));safe(GPIOG,BIT(3));
    SCB->CPACR|=(0xFUL<<20); SCB->VTOR=0x08000000UL; __DSB(); __ISB();
}
/**
 * @brief Bootloader 时钟与中断基础：更新（对应符号 SystemCoreClockUpdate）。
 * @调用关系 直接调用者：未发现普通 C 直接调用；可能由启动文件、中断向量、回调表或外部模块入口调用。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void SystemCoreClockUpdate(void) {SystemCoreClock=16000000U;}
