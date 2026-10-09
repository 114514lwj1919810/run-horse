#include "at24cs64.h"
#include "i2c0.h"
#include "systick.h"
#include <stdio.h>
#include <string.h>
#include "board_timer.h"
#include "board_io.h"
#include "serial_port.h"

int AT24CS64_ReadBuffer(uint16_t addr,uint8_t *data,uint16_t length)
{
    uint8_t a[2]; uint16_t n;
    if(!data || (uint32_t)addr+length>8192) return I2C_ERROR;
    while(length) {
        n=length>32?32:length; a[0]=(uint8_t)(addr>>8); a[1]=(uint8_t)addr;
        if(I2C0_WriteRead(AT24CS64_ADDR,a,2,data,n)!=I2C_OK) return I2C_ERROR;
        addr+=n; data+=n; length-=n; BoardIO_Poll(); SerialPort_Poll();
    }
    return I2C_OK;
}
int AT24CS64_WriteBuffer(uint16_t addr,const uint8_t *data,uint16_t length)
{
    uint8_t page[34]; uint16_t n; uint32_t start;
    if(!data || (uint32_t)addr+length>8192) return I2C_ERROR;
    while(length) {
        n=32-(addr%32); if(n>length) n=length;
        page[0]=(uint8_t)(addr>>8); page[1]=(uint8_t)addr; memcpy(page+2,data,n);
        if(I2C0_WriteByte(AT24CS64_ADDR,page,n+2)!=I2C_OK) return I2C_ERROR;
        start=BoardTimer_Millis();
        while((uint32_t)(BoardTimer_Millis()-start)<5) { BoardIO_Poll(); SerialPort_Poll(); }
        addr+=n; data+=n; length-=n;
    }
    return I2C_OK;
}

int AT24CS64_WriteByte(uint16_t addr, uint8_t data)
{
    uint8_t buf[3];
    int status;

    if(addr > 0x1FFFU)
    {
        return I2C_ERROR;
    }

    /* 16bit Address */

    buf[0] = addr >> 8;
    buf[1] = addr & 0xff;
    buf[2] = data;

    status = I2C0_WriteByte(AT24CS64_ADDR, buf, 3U);
    if(I2C_OK == status)
    {
        /* AT24CS64 maximum self-timed byte-write cycle is 5 ms. */
        delay_1ms(5U);
    }

    return status;
}

int AT24CS64_ReadByte(uint16_t addr, uint8_t *data)
{
    uint8_t tmp[2];

    if((NULL == data) || (addr > 0x1FFFU))
    {
        return I2C_ERROR;
    }

    tmp[0] = addr >> 8;
    tmp[1] = addr & 0xff;

    return I2C0_WriteRead(AT24CS64_ADDR, tmp, 2U, data, 1U);
}

void AT24CS64_test(void)
{
    uint8_t value = 0U;
    uint8_t i;
    int status;

    status = AT24CS64_WriteByte(0x1234U, 0x55U);
    if(I2C_OK == status)
    {
        status = AT24CS64_ReadByte(0x1234U, &value);
    }
    printf("AT24 0x1234: status=%d, value=0x%02X\r\n", status, value);

    for(i = 0U; i < 10U; i++)
    {
        status = AT24CS64_WriteByte(i, (uint8_t)(0x10U + i));
        if(I2C_OK == status)
        {
            status = AT24CS64_ReadByte(i, &value);
        }
        printf("AT24 0x%04X: status=%d, value=0x%02X\r\n",
               i, status, value);
    }
}
