/*!
    \file    input.c
    \brief   check the voltage level of input line.

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/

#include "main.h"

void SW_init(void)
{
    /***************************************************************************
    * SW1-S0  <---> PE2
    * SW1-S0  <---> PE3
    * SW1-S0  <---> PE4
    ***************************************************************************/
    /* enable GPIO Port clock of the SW1 */
    rcu_periph_clock_enable(RCU_GPIOE);

    /* configure the K-INx pin as input */
    gpio_mode_set(GPIOE, GPIO_MODE_INPUT, GPIO_PUPD_PULLUP, GPIO_PIN_2 | GPIO_PIN_3 | GPIO_PIN_4);
}

uint8_t SW_bit_get(void)
{
    uint32_t pin = 0x0;

    /* check S0 of the SW1 */
    if(RESET == gpio_input_bit_get(GPIOE, GPIO_PIN_2))
    {
        pin |= BIT(0);

    }

    /* check S1 of the SW1 */
    if(RESET == gpio_input_bit_get(GPIOE, GPIO_PIN_3))
    {
        pin |= BIT(1);
    }

    /* check S2 of the SW1 */
    if(RESET == gpio_input_bit_get(GPIOE, GPIO_PIN_4))
    {
        pin |= BIT(2);
    }

    return pin;
}

void SW_test(void)
{
    uint8_t sw = 0;
    sw = SW_bit_get();
    printf("sw = %d \r\n", sw);
    OUT_bit_write(sw);
    delay_1ms(500);
}

void IN_init(void)
{
    /***************************************************************************
    * K-IN1  <---> PA11
    * K-IN2  <---> PC9
    * K-IN3  <---> PC8
    * K-IN4  <---> PD15
    * K-IN5  <---> PD14
    * K-IN6  <---> PD13
    * K-IN7  <---> PD12
    * K-IN8  <---> PD11
    * K-IN9  <---> PD10
    * K-IN10 <---> PD9
    * K-IN11 <---> PD8
    * K-IN12 <---> PB15
    * K-IN13 <---> PB14
    * K-IN14 <---> PB13
    * K-IN15 <---> PB12
    * K-IN16 <---> PE15
    * K-IN17 <---> PE14
    * K-IN18 <---> PE13
    * K-IN19 <---> PE12
    * K-IN20 <---> PE11
    * K-IN21 <---> PE10
    * K-IN22 <---> PE9
    * K-IN23 <---> PE8
    * K-IN24 <---> PE7
    * K-IN25 <---> PB1
    * K-IN26 <---> PB0
    * K-IN27 <---> PC5 (Backup)
    * K-IN28 <---> PC4 (Backup)
    ***************************************************************************/

    /* enable GPIO Port clock of the K-INx */
    rcu_periph_clock_enable(RCU_GPIOA);
    rcu_periph_clock_enable(RCU_GPIOB);
    rcu_periph_clock_enable(RCU_GPIOC);
    rcu_periph_clock_enable(RCU_GPIOD);
    rcu_periph_clock_enable(RCU_GPIOE);

    /* configure the K-INx pin as input */
    gpio_mode_set(GPIOA, GPIO_MODE_INPUT, GPIO_PUPD_NONE, GPIO_PIN_11);
    gpio_mode_set(GPIOB, GPIO_MODE_INPUT, GPIO_PUPD_NONE, GPIO_PIN_15 | GPIO_PIN_14 | GPIO_PIN_13 | GPIO_PIN_12 | GPIO_PIN_1  | GPIO_PIN_0);
    gpio_mode_set(GPIOC, GPIO_MODE_INPUT, GPIO_PUPD_NONE, GPIO_PIN_9  | GPIO_PIN_8  | GPIO_PIN_5  | GPIO_PIN_4);
    gpio_mode_set(GPIOD, GPIO_MODE_INPUT, GPIO_PUPD_NONE, GPIO_PIN_15 | GPIO_PIN_14 | GPIO_PIN_13 | GPIO_PIN_12 | GPIO_PIN_11 | GPIO_PIN_10 | GPIO_PIN_9 | GPIO_PIN_8);
    gpio_mode_set(GPIOE, GPIO_MODE_INPUT, GPIO_PUPD_NONE, GPIO_PIN_15 | GPIO_PIN_14 | GPIO_PIN_13 | GPIO_PIN_12 | GPIO_PIN_11 | GPIO_PIN_10 | GPIO_PIN_9 | GPIO_PIN_8 | GPIO_PIN_7);
}

/*!
    \brief    get the bit of IN port.
    \param[in]  none.
    \retval     pin: the bit of IN port.
                one or more parameters can be selected which are shown as below:
      \arg        GPIO_PIN_x(x=0..15), GPIO_PIN_ALL
*/
uint32_t IN_bit_get(void)
{
    uint32_t pin = 0;
    uint8_t i;

    for( i = 0; i < 28; i++)
    {
        switch(i)
        {
            case 0: /* K-IN1  <---> PA11 */
                if(SET == gpio_input_bit_get(GPIOA, GPIO_PIN_11))
                {
                    pin |= BIT(i);
                }

                break;

            case 1: /* K-IN2  <---> PC9 */
                if(SET == gpio_input_bit_get(GPIOC, GPIO_PIN_9))
                {
                    pin |= BIT(i);
                }

                break;

            case 2: /* K-IN3  <---> PC8 */
                if(SET == gpio_input_bit_get(GPIOC, GPIO_PIN_8))
                {
                    pin |= BIT(i);
                }

                break;

            case 3: /* K-IN4  <---> PD15 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_15))
                {
                    pin |= BIT(i);
                }

                break;

            case 4: /* K-IN5  <---> PD14 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_14))
                {
                    pin |= BIT(i);
                }

                break;

            case 5: /* K-IN6  <---> PD13 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_13))
                {
                    pin |= BIT(i);
                }

                break;

            case 6: /* K-IN7  <---> PD12 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_12))
                {
                    pin |= BIT(i);
                }

                break;

            case 7: /* K-IN8  <---> PD11 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_11))
                {
                    pin |= BIT(i);
                }

                break;

            case 8: /* K-IN9  <---> PD10 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_10))
                {
                    pin |= BIT(i);
                }

                break;

            case 9: /* K-IN10 <---> PD9 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_9))
                {
                    pin |= BIT(i);
                }

                break;

            case 10: /* K-IN11 <---> PD8 */
                if(SET == gpio_input_bit_get(GPIOD, GPIO_PIN_8))
                {
                    pin |= BIT(i);
                }

                break;

            case 11: /* K-IN12 <---> PB15 */
                if(SET == gpio_input_bit_get(GPIOB, GPIO_PIN_15))
                {
                    pin |= BIT(i);
                }

                break;

            case 12: /* K-IN13 <---> PB14 */
                if(SET == gpio_input_bit_get(GPIOB, GPIO_PIN_14))
                {
                    pin |= BIT(i);
                }

                break;

            case 13: /* K-IN14 <---> PB13 */
                if(SET == gpio_input_bit_get(GPIOB, GPIO_PIN_13))
                {
                    pin |= BIT(i);
                }

                break;

            case 14: /* K-IN15 <---> PB12 */
                if(SET == gpio_input_bit_get(GPIOB, GPIO_PIN_12))
                {
                    pin |= BIT(i);
                }

                break;

            case 15: /* K-IN16 <---> PE15 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_15))
                {
                    pin |= BIT(i);
                }

                break;

            case 16: /* K-IN17 <---> PE14 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_14))
                {
                    pin |= BIT(i);
                }

                break;

            case 17: /* K-IN18 <---> PE13 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_13))
                {
                    pin |= BIT(i);
                }

                break;

            case 18: /* K-IN19 <---> PE12 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_12))
                {
                    pin |= BIT(i);
                }

                break;

            case 19: /* K-IN20 <---> PE11 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_11))
                {
                    pin |= BIT(i);
                }

                break;

            case 20: /* K-IN21 <---> PE10 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_10))
                {
                    pin |= BIT(i);
                }

                break;

            case 21: /* K-IN22 <---> PE9 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_9))
                {
                    pin |= BIT(i);
                }

                break;

            case 22: /* K-IN23 <---> PE8 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_8))
                {
                    pin |= BIT(i);
                }

                break;

            case 23: /* K-IN24 <---> PE7 */
                if(SET == gpio_input_bit_get(GPIOE, GPIO_PIN_7))
                {
                    pin |= BIT(i);
                }

                break;

            case 24: /* K-IN25 <---> PB1 */
                if(SET == gpio_input_bit_get(GPIOB, GPIO_PIN_1))
                {
                    pin |= BIT(i);
                }

                break;

            case 25: /* K-IN26 <---> PB0 */
                if(SET == gpio_input_bit_get(GPIOB, GPIO_PIN_0))
                {
                    pin |= BIT(i);
                }

                break;

            case 26: /* K-IN27 <---> PC5 (Backup) */
                if(SET == gpio_input_bit_get(GPIOC, GPIO_PIN_5))
                {
                    pin |= BIT(i);
                }

                break;

            case 27: /* K-IN28 <---> PC4 (Backup) */
                if(SET == gpio_input_bit_get(GPIOC, GPIO_PIN_4))
                {
                    pin |= BIT(i);
                }

                break;
        }
    }

    return pin & 0xFFFFFFF;
}

void IN_test(void)
{
    uint32_t in = 0;

    in = IN_bit_get();
    printf("IN = %0x \r\n", in);
    OUT_bit_write(in);
    delay_1ms(500);
}
