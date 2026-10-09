#include "i2c0.h"
#include "systick.h"

static int I2C0_CheckError(void)
{
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_AERR))
    {
        return I2C_NACK;
    }
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_BERR))
    {
        return I2C_BUS_ERR;
    }
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_LOSTARB))
    {
        return I2C_ARB_LOST;
    }
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_OUERR))
    {
        return I2C_OVERRUN;
    }

    return I2C_OK;
}

static int I2C0_WaitFlagSet(i2c_flag_enum flag)
{
    uint32_t timeout = I2C_TIMEOUT_MAX;
    int status;

    while(1)
    {
        /* Check errors before the target flag: both can be set together. */
        status = I2C0_CheckError();
        if(I2C_OK != status)
        {
            return status;
        }

        if(SET == i2c_flag_get(I2C0, flag))
        {
            return I2C_OK;
        }

        if(0U == timeout--)
        {
            return I2C_TIMEOUT;
        }
    }
}

static int I2C0_WaitBusIdle(void)
{
    uint32_t timeout = I2C_TIMEOUT_MAX;

    while(SET == i2c_flag_get(I2C0, I2C_FLAG_I2CBSY))
    {
        if(0U == timeout--)
        {
            return I2C_TIMEOUT;
        }
    }

    return I2C_OK;
}

static int I2C0_WaitStopCleared(void)
{
    uint32_t timeout = I2C_TIMEOUT_MAX;

    while(0U != (I2C_CTL0(I2C0) & I2C_CTL0_STOP))
    {
        if(0U == timeout--)
        {
            return I2C_TIMEOUT;
        }
    }

    return I2C_OK;
}

static void I2C0_ClearErrors(void)
{
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_AERR))
    {
        i2c_flag_clear(I2C0, I2C_FLAG_AERR);
    }
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_BERR))
    {
        i2c_flag_clear(I2C0, I2C_FLAG_BERR);
    }
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_LOSTARB))
    {
        i2c_flag_clear(I2C0, I2C_FLAG_LOSTARB);
    }
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_OUERR))
    {
        i2c_flag_clear(I2C0, I2C_FLAG_OUERR);
    }
}

void I2C0_Init(void)
{
    rcu_periph_clock_enable(RCU_GPIOB);
    rcu_periph_clock_enable(RCU_I2C0);

    gpio_af_set(GPIOB, GPIO_AF_4, GPIO_PIN_6 | GPIO_PIN_7);
    gpio_mode_set(GPIOB, GPIO_MODE_AF, GPIO_PUPD_PULLUP,
                  GPIO_PIN_6 | GPIO_PIN_7);
    gpio_output_options_set(GPIOB, GPIO_OTYPE_OD, GPIO_OSPEED_50MHZ,
                            GPIO_PIN_6 | GPIO_PIN_7);

    i2c_deinit(I2C0);
    i2c_clock_config(I2C0, I2C_SPEED, I2C_DTCY_2);
    i2c_mode_addr_config(I2C0, I2C_I2CMODE_ENABLE,
                         I2C_ADDFORMAT_7BITS, 0U);
    i2c_enable(I2C0);
    i2c_ackpos_config(I2C0, I2C_ACKPOS_CURRENT);
    i2c_ack_config(I2C0, I2C_ACK_ENABLE);
}

void I2C0_BusReset(void)
{
    uint8_t i;

    i2c_disable(I2C0);

    gpio_mode_set(GPIOB, GPIO_MODE_OUTPUT, GPIO_PUPD_PULLUP,
                  GPIO_PIN_6 | GPIO_PIN_7);
    gpio_output_options_set(GPIOB, GPIO_OTYPE_OD, GPIO_OSPEED_50MHZ,
                            GPIO_PIN_6 | GPIO_PIN_7);

    /* Release both lines, then clock a target that may be holding SDA low. */
    gpio_bit_set(GPIOB, GPIO_PIN_6 | GPIO_PIN_7);
    delay_1us(5U);

    for(i = 0U; i < 9U; i++)
    {
        gpio_bit_reset(GPIOB, GPIO_PIN_6);
        delay_1us(5U);
        gpio_bit_set(GPIOB, GPIO_PIN_6);
        delay_1us(5U);
    }

    /* Generate a STOP: SDA low -> SCL high -> SDA high. */
    gpio_bit_reset(GPIOB, GPIO_PIN_7);
    delay_1us(5U);
    gpio_bit_set(GPIOB, GPIO_PIN_6);
    delay_1us(5U);
    gpio_bit_set(GPIOB, GPIO_PIN_7);
    delay_1us(5U);

    I2C0_Init();
}

static int I2C0_Prepare(void)
{
    int status;

    i2c_ackpos_config(I2C0, I2C_ACKPOS_CURRENT);
    i2c_ack_config(I2C0, I2C_ACK_ENABLE);
    I2C0_ClearErrors();

    status = I2C0_WaitBusIdle();
    if(I2C_OK != status)
    {
        I2C0_BusReset();
        status = I2C0_WaitBusIdle();
    }

    return status;
}

static void I2C0_Abort(void)
{
    if(SET == i2c_flag_get(I2C0, I2C_FLAG_MASTER))
    {
        i2c_stop_on_bus(I2C0);
    }

    I2C0_ClearErrors();
    i2c_ackpos_config(I2C0, I2C_ACKPOS_CURRENT);
    i2c_ack_config(I2C0, I2C_ACK_ENABLE);
    I2C0_BusReset();
}

static int I2C0_StartAddress(uint8_t dev_addr, uint32_t direction)
{
    int status;

    i2c_start_on_bus(I2C0);
    status = I2C0_WaitFlagSet(I2C_FLAG_SBSEND);
    if(I2C_OK != status)
    {
        return status;
    }

    /* The public API accepts a 7-bit address; the peripheral expects it shifted. */
    i2c_master_addressing(I2C0, (uint32_t)dev_addr << 1U, direction);
    return I2C0_WaitFlagSet(I2C_FLAG_ADDSEND);
}

static int I2C0_Transmit(const uint8_t *buf, uint16_t len)
{
    uint16_t i;
    int status;

    i2c_flag_clear(I2C0, I2C_FLAG_ADDSEND);

    for(i = 0U; i < len; i++)
    {
        status = I2C0_WaitFlagSet(I2C_FLAG_TBE);
        if(I2C_OK != status)
        {
            return status;
        }
        i2c_data_transmit(I2C0, buf[i]);

        /* Wait for the byte and its ACK before loading the next byte. */
        status = I2C0_WaitFlagSet(I2C_FLAG_BTC);
        if(I2C_OK != status)
        {
            return status;
        }
    }

    return I2C_OK;
}

static int I2C0_Receive(uint8_t *buf, uint16_t len)
{
    uint16_t remaining = len;
    uint16_t index = 0U;
    int status;

    if(1U == remaining)
    {
        /* For one byte, program NACK before clearing ADDSEND/ADDR. */
        i2c_ack_config(I2C0, I2C_ACK_DISABLE);
        i2c_flag_clear(I2C0, I2C_FLAG_ADDSEND);
        i2c_stop_on_bus(I2C0);

        status = I2C0_WaitFlagSet(I2C_FLAG_RBNE);
        if(I2C_OK == status)
        {
            buf[0] = i2c_data_receive(I2C0);
        }
        return status;
    }

    if(2U == remaining)
    {
        i2c_ackpos_config(I2C0, I2C_ACKPOS_NEXT);
        i2c_ack_config(I2C0, I2C_ACK_DISABLE);
        i2c_flag_clear(I2C0, I2C_FLAG_ADDSEND);

        status = I2C0_WaitFlagSet(I2C_FLAG_BTC);
        if(I2C_OK != status)
        {
            return status;
        }

        i2c_stop_on_bus(I2C0);
        buf[0] = i2c_data_receive(I2C0);
        buf[1] = i2c_data_receive(I2C0);
        return I2C_OK;
    }

    i2c_flag_clear(I2C0, I2C_FLAG_ADDSEND);

    while(remaining > 3U)
    {
        status = I2C0_WaitFlagSet(I2C_FLAG_RBNE);
        if(I2C_OK != status)
        {
            return status;
        }
        buf[index++] = i2c_data_receive(I2C0);
        remaining--;
    }

    /* Three bytes remain: ACK the first, then NACK the final byte. */
    status = I2C0_WaitFlagSet(I2C_FLAG_BTC);
    if(I2C_OK != status)
    {
        return status;
    }
    i2c_ack_config(I2C0, I2C_ACK_DISABLE);
    buf[index++] = i2c_data_receive(I2C0);

    status = I2C0_WaitFlagSet(I2C_FLAG_BTC);
    if(I2C_OK != status)
    {
        return status;
    }
    i2c_stop_on_bus(I2C0);
    buf[index++] = i2c_data_receive(I2C0);
    buf[index] = i2c_data_receive(I2C0);

    return I2C_OK;
}

int I2C0_WriteByte(uint8_t dev_addr, const uint8_t *buf, uint16_t len)
{
    int status;

    if((NULL == buf) || (0U == len) || (dev_addr > 0x7FU))
    {
        return I2C_ERROR;
    }

    status = I2C0_Prepare();
    if(I2C_OK == status)
    {
        status = I2C0_StartAddress(dev_addr, I2C_TRANSMITTER);
    }
    if(I2C_OK == status)
    {
        status = I2C0_Transmit(buf, len);
    }
    if(I2C_OK == status)
    {
        i2c_stop_on_bus(I2C0);
        status = I2C0_WaitStopCleared();
    }

    if(I2C_OK != status)
    {
        I2C0_Abort();
    }

    return status;
}

int I2C0_ReadByte(uint8_t dev_addr, uint8_t *buf, uint16_t len)
{
    int status;

    if((NULL == buf) || (0U == len) || (dev_addr > 0x7FU))
    {
        return I2C_ERROR;
    }

    status = I2C0_Prepare();
    if(I2C_OK == status)
    {
        status = I2C0_StartAddress(dev_addr, I2C_RECEIVER);
    }
    if(I2C_OK == status)
    {
        status = I2C0_Receive(buf, len);
    }
    if(I2C_OK == status)
    {
        status = I2C0_WaitStopCleared();
    }

    i2c_ackpos_config(I2C0, I2C_ACKPOS_CURRENT);
    i2c_ack_config(I2C0, I2C_ACK_ENABLE);

    if(I2C_OK != status)
    {
        I2C0_Abort();
    }

    return status;
}

int I2C0_WriteRead(uint8_t dev_addr,
                   const uint8_t *write_buf, uint16_t write_len,
                   uint8_t *read_buf, uint16_t read_len)
{
    int status;

    if((NULL == write_buf) || (0U == write_len) ||
       (NULL == read_buf) || (0U == read_len) || (dev_addr > 0x7FU))
    {
        return I2C_ERROR;
    }

    status = I2C0_Prepare();
    if(I2C_OK == status)
    {
        status = I2C0_StartAddress(dev_addr, I2C_TRANSMITTER);
    }
    if(I2C_OK == status)
    {
        status = I2C0_Transmit(write_buf, write_len);
    }
    if(I2C_OK == status)
    {
        /* Repeated START is required by EEPROM random reads and PCF8563. */
        status = I2C0_StartAddress(dev_addr, I2C_RECEIVER);
    }
    if(I2C_OK == status)
    {
        status = I2C0_Receive(read_buf, read_len);
    }
    if(I2C_OK == status)
    {
        status = I2C0_WaitStopCleared();
    }

    i2c_ackpos_config(I2C0, I2C_ACKPOS_CURRENT);
    i2c_ack_config(I2C0, I2C_ACK_ENABLE);

    if(I2C_OK != status)
    {
        I2C0_Abort();
    }

    return status;
}

int I2C0_Probe(uint8_t dev_addr)
{
    int status;

    if(dev_addr > 0x7FU)
    {
        return I2C_ERROR;
    }

    status = I2C0_Prepare();
    if(I2C_OK == status)
    {
        status = I2C0_StartAddress(dev_addr, I2C_TRANSMITTER);
    }

    if(I2C_OK == status)
    {
        i2c_flag_clear(I2C0, I2C_FLAG_ADDSEND);
        i2c_stop_on_bus(I2C0);
        status = I2C0_WaitStopCleared();
    }
    else if(I2C_NACK == status)
    {
        /* A missing address is normal during a scan; do not reset the bus. */
        i2c_stop_on_bus(I2C0);
        i2c_flag_clear(I2C0, I2C_FLAG_AERR);
        (void)I2C0_WaitStopCleared();
    }
    else
    {
        I2C0_Abort();
    }

    return status;
}
