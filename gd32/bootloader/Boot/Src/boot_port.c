/* AUTO-FILE-DOC
 * 文件职责：Bootloader 硬件适配。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#include "boot_config.h"
#include "boot_spi1.h"
static volatile uint32_t ticks;
/**
 * @brief Bootloader 硬件适配：处理状态或事件（对应符号 SysTick_Handler）。
 * @调用关系 直接调用者：未发现普通 C 直接调用；可能由启动文件、中断向量、回调表或外部模块入口调用。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void SysTick_Handler(void) { ++ticks; }
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_now）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_wait_ready、boot_sd.c:boot_sd_init、boot_sd.c:boot_sd_read_sector。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
uint32_t boot_now(void) { return ticks; }
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_watchdog_refresh）。
 * @调用关系 直接调用者：boot_fat32.c:boot_fat32_find_in_dir、boot_fat32.c:boot_fat32_read、boot_sd.c:boot_sd_wait_ready、boot_sd.c:boot_sd_send_cmd、boot_sd.c:boot_sd_init、boot_sd.c:boot_sd_read_sector。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_watchdog_refresh(void) { fwdgt_counter_reload(); }
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 HAL_Delay）。
 * @调用关系 直接调用者：boot_main.c:main、boot_port.c:boot_led、boot_sd.c:boot_sd_init。
 * @局部状态 主要临时变量：t；均只在本次调用中有效。
 * @param ms 按值传入；函数内部修改不会回写调用方变量。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void HAL_Delay(uint32_t ms) { uint32_t t=ticks; while((uint32_t)(ticks-t)<ms) fwdgt_counter_reload(); }
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 low）。
 * @调用关系 直接调用者：boot_port.c:boot_port_init。
 * @param port 按值传入；函数内部修改不会回写调用方变量。
 * @param pins 按值传入；函数内部修改不会回写调用方变量。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
/**
 * @brief Bootloader 硬件适配：初始化（对应符号 boot_port_init）。
 * @调用关系 直接调用者：boot_main.c:main。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_port_init(void) {
    rcu_periph_clock_enable(RCU_GPIOA); rcu_periph_clock_enable(RCU_GPIOB);
    rcu_periph_clock_enable(RCU_GPIOC);
    gpio_bit_reset(GPIOC,GPIO_PIN_13);
    gpio_output_options_set(GPIOC,GPIO_OTYPE_PP,GPIO_OSPEED_2MHZ,GPIO_PIN_13);
    gpio_mode_set(GPIOC,GPIO_MODE_OUTPUT,GPIO_PUPD_PULLDOWN,GPIO_PIN_13);
    fwdgt_config(4095U,FWDGT_PSC_DIV256); fwdgt_enable();
    SysTick_Config(SystemCoreClock/1000U); __enable_irq();
#if defined(BOOT_DRY_RUN) || defined(BOOT_TRACE)
    /* Type-C/CH340 debug UART: PA9 = USART0_TX, 115200 8N1. */
    rcu_periph_clock_enable(RCU_USART0);
    gpio_af_set(GPIOA,GPIO_AF_7,GPIO_PIN_9);
    gpio_mode_set(GPIOA,GPIO_MODE_AF,GPIO_PUPD_PULLUP,GPIO_PIN_9);
    gpio_output_options_set(GPIOA,GPIO_OTYPE_PP,GPIO_OSPEED_50MHZ,GPIO_PIN_9);
    usart_deinit(USART0); usart_baudrate_set(USART0,115200U);
    usart_word_length_set(USART0,USART_WL_8BIT); usart_stop_bit_set(USART0,USART_STB_1BIT);
    usart_parity_config(USART0,USART_PM_NONE); usart_transmit_config(USART0,USART_TRANSMIT_ENABLE); usart_enable(USART0);
#endif
}
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_log）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_main.c:adopt_factory、boot_main.c:main。
 * @局部状态 主要临时变量：t；均只在本次调用中有效。
 * @param s 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_log(const char *s) {
#if defined(BOOT_DRY_RUN) || defined(BOOT_TRACE)
    uint32_t t;
    while(*s) { t=ticks; while(RESET==usart_flag_get(USART0,USART_FLAG_TBE)) if(ticks-t>20U)return;
        usart_data_transmit(USART0,(uint8_t)*s++); }
    t=ticks; while(RESET==usart_flag_get(USART0,USART_FLAG_TC)) if(ticks-t>20U)return;
#else
    (void)s;
#endif
}
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_led）。
 * @调用关系 直接调用者：boot_main.c:try_update、boot_main.c:main。
 * @param count 按值传入；函数内部修改不会回写调用方变量。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_led(unsigned count) {
    while(count--) { gpio_bit_set(GPIOC,GPIO_PIN_13); HAL_Delay(100); gpio_bit_reset(GPIOC,GPIO_PIN_13); HAL_Delay(150); }
}
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_spi1_shared_bus_idle）。
 * @调用关系 直接调用者：boot_port.c:boot_spi1_gpio_init、boot_port.c:boot_jump、boot_sd.c:boot_sd_init。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_spi1_shared_bus_idle(void) { gpio_bit_set(GPIOA,GPIO_PIN_4); }
/**
 * @brief Bootloader 硬件适配：初始化（对应符号 boot_spi1_gpio_init）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_init。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_spi1_gpio_init(void) {
    rcu_periph_clock_enable(RCU_SPI0); boot_spi1_shared_bus_idle();
    gpio_mode_set(GPIOA,GPIO_MODE_OUTPUT,GPIO_PUPD_PULLUP,GPIO_PIN_4);
    gpio_output_options_set(GPIOA,GPIO_OTYPE_PP,GPIO_OSPEED_50MHZ,GPIO_PIN_4);
    gpio_af_set(GPIOA,GPIO_AF_5,GPIO_PIN_5|GPIO_PIN_6|GPIO_PIN_7);
    gpio_mode_set(GPIOA,GPIO_MODE_AF,GPIO_PUPD_PULLUP,GPIO_PIN_5|GPIO_PIN_6|GPIO_PIN_7);
    gpio_output_options_set(GPIOA,GPIO_OTYPE_PP,GPIO_OSPEED_50MHZ,GPIO_PIN_5|GPIO_PIN_6|GPIO_PIN_7);
}
/**
 * @brief Bootloader 硬件适配：设置（对应符号 spi_setup）。
 * @调用关系 直接调用者：boot_port.c:boot_spi1_init_slow、boot_port.c:boot_spi1_init_fast。
 * @param divider 按值传入；函数内部修改不会回写调用方变量。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
static void spi_setup(uint32_t divider) {
    spi_parameter_struct p; spi_i2s_deinit(SPI0); spi_struct_para_init(&p);
    p.device_mode=SPI_MASTER; p.trans_mode=SPI_TRANSMODE_FULLDUPLEX; p.frame_size=SPI_FRAMESIZE_8BIT;
    p.clock_polarity_phase=SPI_CK_PL_LOW_PH_1EDGE; p.nss=SPI_NSS_SOFT; p.prescale=divider; p.endian=SPI_ENDIAN_MSB;
    spi_init(SPI0,&p); spi_enable(SPI0);
}
/**
 * @brief Bootloader 硬件适配：初始化（对应符号 boot_spi1_init_slow）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_init。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_spi1_init_slow(void) { spi_setup(SPI_PSC_256); } /* IRC16M / 256 = 62.5kHz */
/**
 * @brief Bootloader 硬件适配：初始化（对应符号 boot_spi1_init_fast）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_init。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_spi1_init_fast(void) { spi_setup(SPI_PSC_16); }
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_spi1_transfer）。
 * @调用关系 直接调用者：boot_port.c:boot_spi1_transfer_buffer、boot_port.c:boot_tf_unselect、boot_sd.c:boot_sd_wait_ready、boot_sd.c:boot_sd_send_cmd、boot_sd.c:boot_sd_init、boot_sd.c:boot_sd_read_sector。
 * @局部状态 主要临时变量：t；均只在本次调用中有效。
 * @param data 按值传入；函数内部修改不会回写调用方变量。
 * @return 返回值含义由声明类型和调用处分支共同约定；0/1 通常表示失败/成功或否/是。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
uint8_t boot_spi1_transfer(uint8_t data) {
    uint32_t t=ticks;
    while(RESET==spi_i2s_flag_get(SPI0,SPI_FLAG_TBE)) if(ticks-t>5U)return 0xFF;
    spi_i2s_data_transmit(SPI0,data); t=ticks;
    while(RESET==spi_i2s_flag_get(SPI0,SPI_FLAG_RBNE)) if(ticks-t>5U)return 0xFF;
    return (uint8_t)spi_i2s_data_receive(SPI0);
}
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_spi1_transfer_buffer）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_read_sector。
 * @局部状态 主要临时变量：b；均只在本次调用中有效。
 * @param tx 只读输入指针；调用方必须保证地址有效且容量满足接口约定。
 * @param rx 输入/输出指针（函数可能通过它回写结果）；调用方必须保证地址有效且容量满足接口约定。
 * @param n 按值传入；函数内部修改不会回写调用方变量。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_spi1_transfer_buffer(const uint8_t *tx,uint8_t *rx,uint32_t n) {
    uint8_t b; while(n--) {b=boot_spi1_transfer(tx?*tx++:0xFF); if(rx)*rx++=b;}
}
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_tf_select）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_send_cmd。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_tf_select(void) { gpio_bit_reset(GPIOA,GPIO_PIN_4); }
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_tf_unselect）。
 * @调用关系 直接调用者：boot_sd.c:boot_sd_send_cmd、boot_sd.c:boot_sd_end_cmd。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_tf_unselect(void) { gpio_bit_set(GPIOA,GPIO_PIN_4); (void)boot_spi1_transfer(0xFF); }
/* Naked tail branch: never use the old C stack after replacing MSP. */
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_branch）。
 * @调用关系 直接调用者：boot_port.c:boot_jump。
 * @局部状态 主要临时变量：MSP；均只在本次调用中有效。
 * @param sp 按值传入；函数内部修改不会回写调用方变量。
 * @param entry 按值传入；函数内部修改不会回写调用方变量。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
__asm void boot_branch(uint32_t sp,uint32_t entry) { MSR MSP,r0
    BX r1
}
/**
 * @brief Bootloader 硬件适配：执行内部处理（对应符号 boot_jump）。
 * @调用关系 直接调用者：boot_main.c:main。
 * @param base 按值传入；函数内部修改不会回写调用方变量。
 * @note 位移及按位与/或用于寄存器、掩码或小端字段拼装，不是逻辑与/或。
 * @note AUTO-DOC：调用关系由本工程源码静态检索生成；硬件向量和函数指针调用需结合工程文件确认。
 */
void boot_jump(uint32_t base) {
    unsigned i; uint32_t sp=*(uint32_t *)base, entry=*(uint32_t *)(base+4U);
    boot_spi1_shared_bus_idle(); spi_disable(SPI0);
#if defined(BOOT_DRY_RUN) || defined(BOOT_TRACE)
    usart_disable(USART0);
#endif
    __disable_irq(); SysTick->CTRL=0U; SysTick->LOAD=0U; SysTick->VAL=0U;
    for(i=0;i<8;i++) {NVIC->ICER[i]=0xFFFFFFFFUL; NVIC->ICPR[i]=0xFFFFFFFFUL;}
    SCB->ICSR=SCB_ICSR_PENDSTCLR_Msk|SCB_ICSR_PENDSVCLR_Msk;
    SCB->VTOR=base; __set_CONTROL(0U); __DSB(); __ISB(); fwdgt_counter_reload();
    /* A hardware reset enters the application with PRIMASK clear.  Match that
     * state after the software jump so SysTick and peripheral IRQs can run. */
    __enable_irq();
    boot_branch(sp,entry);
    while(1) {}
}
