#include "board_timer.h"
#include "systick.h"

static uint32_t s_last_10ms_tick;
static uint32_t s_last_1s_tick;

void BoardTimer_Init(void)
{
    uint32_t now;

    now = systick_millis();
    s_last_10ms_tick = now;
    s_last_1s_tick = now;
}

void BoardTimer_OnTim6Irq(void)
{
}

uint32_t BoardTimer_Millis(void)
{
    return systick_millis();
}

uint8_t BoardTimer_Consume10msFlag(void)
{
    uint32_t now;

    now = systick_millis();
    if((now - s_last_10ms_tick) < 10U)
    {
        return 0U;
    }

    s_last_10ms_tick += 10U;
    return 1U;
}

uint8_t BoardTimer_Consume1sFlag(void)
{
    uint32_t now;

    now = systick_millis();
    if((now - s_last_1s_tick) < 1000U)
    {
        return 0U;
    }

    s_last_1s_tick += 1000U;
    return 1U;
}
