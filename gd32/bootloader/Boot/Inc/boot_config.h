/* AUTO-FILE-DOC
 * 文件职责：boot_config 模块。
 * 阅读方法：函数注释列出直接调用者、参数/指针方向和关键位运算；文件级变量的完整引用位置见工程根目录《代码阅读说明.md》。
 * 范围声明：本注释描述当前源码，不替代原理图、电气极性和硬件实测。
 */
#ifndef GD_BOOT_CONFIG_H
#define GD_BOOT_CONFIG_H
#include "gd32f4xx.h"
#include "boot_contract.h"
#define BOOT_TF_SECTOR_SIZE 512U
void HAL_Delay(uint32_t ms);
uint32_t boot_now(void);
void boot_log(const char *s);
void boot_led(unsigned count);
#endif
