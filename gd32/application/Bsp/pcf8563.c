#include "pcf8563.h"
#include "i2c0.h"
#include "systick.h"
#include <stdio.h>

uint8_t BCD2DEC(uint8_t bcd)
{
    return (bcd >> 4) * 10 + (bcd & 0x0f);
}

uint8_t DEC2BCD(uint8_t dec)
{
    return ((dec / 10) << 4) + (dec % 10);
}

int PCF8563_SetTime(uint8_t year, uint8_t mon, uint8_t day,
                    uint8_t hour, uint8_t min, uint8_t sec)
{
    uint8_t buf[10];

    if((year > 99U) || (mon < 1U) || (mon > 12U) ||
       (day < 1U) || (day > 31U) || (hour > 23U) ||
       (min > 59U) || (sec > 59U))
    {
        return I2C_ERROR;
    }

    /* Write control and all clock registers in one atomic transaction. */
    buf[0] = 0x00U;
    buf[1] = 0x00U;          /* Control_status_1: clock running */
    buf[2] = 0x00U;          /* Control_status_2 */
    buf[3] = DEC2BCD(sec);
    buf[4] = DEC2BCD(min);
    buf[5] = DEC2BCD(hour);
    buf[6] = DEC2BCD(day);
    buf[7] = 0x00U;          /* weekday is not supplied by this API */
    buf[8] = DEC2BCD(mon);
    buf[9] = DEC2BCD(year);

    return I2C0_WriteByte(PCF8563_ADDR, buf, 10U);
}

int PCF8563_GetTime(uint8_t *hour, uint8_t *min, uint8_t *sec)
{
    uint8_t reg;
    uint8_t data;
    int status;

    if((NULL == hour) || (NULL == min) || (NULL == sec))
    {
        return I2C_ERROR;
    }

    /* Use the proven one-byte receive sequence for each register. */
    reg = PCF8563_SEC;
    status = I2C0_WriteRead(PCF8563_ADDR, &reg, 1U, &data, 1U);
    if(I2C_OK != status)
    {
        return status;
    }
    *sec = BCD2DEC(data & 0x7FU);

    reg = PCF8563_MIN;
    status = I2C0_WriteRead(PCF8563_ADDR, &reg, 1U, &data, 1U);
    if(I2C_OK != status)
    {
        return status;
    }
    *min = BCD2DEC(data & 0x7FU);

    reg = PCF8563_HOUR;
    status = I2C0_WriteRead(PCF8563_ADDR, &reg, 1U, &data, 1U);
    if(I2C_OK != status)
    {
        return status;
    }
    *hour = BCD2DEC(data & 0x3FU);

    return I2C_OK;
}

void PCF8563_test(void)
{
    uint8_t h, m, s;
    int status;

    //26-08-09 20:30:00
    status = PCF8563_SetTime(26U, 8U, 9U, 20U, 30U, 0U);
    printf("PCF8563 set: status=%d\r\n", status);

    while(1)
    {
        status = PCF8563_GetTime(&h, &m, &s);
        if(I2C_OK == status)
        {
            printf("%02d:%02d:%02d\r\n", h, m, s);
        }
        else
        {
            printf("PCF8563 read error: %d\r\n", status);
        }
        delay_1ms(1000U);
    }
}
