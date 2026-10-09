/*!
    \file    output.c
    \brief   output high level or low level

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/

#include "gd32f4xx.h"
#include "output.h"

void LED1_init(void)
{
    /***************************************************************************
    * LED1 <---> PC13
    ***************************************************************************/

    /* configure LED1 GPIO port */
    gpio_mode_set(GPIOC, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO_PIN_13);
    gpio_output_options_set(GPIOC, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_13);
    /* reset LED1 GPIO pin, turn on LED */
    gpio_bit_reset(GPIOC, GPIO_PIN_13);
}

void LED1_on(void)
{
    gpio_bit_reset(GPIOC, GPIO_PIN_13);
}

void LED1_off(void)
{
    gpio_bit_set(GPIOC, GPIO_PIN_13);
}

void LED1_flash(uint32_t ms)
{
    /* turn on LED1 */
    LED1_on();
    delay_1ms(ms);

    /* turn off LED1 */
    LED1_off();
    delay_1ms(ms);
}

void OUT_init(void)
{
    /***************************************************************************
    * OUT_H1  <---> PE1
    * OUT_H2  <---> PE0
    * OUT_H3  <---> PB9
    * OUT_H4  <---> PB8
    * OUT_H5  <---> PB5
    * OUT_H6  <---> PD7
    * OUT_H7  <---> PD6
    * OUT_H8  <---> PD5
    * OUT_H9  <---> PD4
    * OUT_H10 <---> PD3
    * OUT_H11 <---> PD1
    * OUT_H12 <---> PD0
    * OUT_H13 <---> PC11
    * OUT_H14 <---> PC10
    * OUT_H15 <---> PA15
    * OUT_H16 <---> PA12
    ***************************************************************************/

    /* enable the OUT-Hx GPIO clock */
    rcu_periph_clock_enable(RCU_GPIOA);
    rcu_periph_clock_enable(RCU_GPIOB);
    rcu_periph_clock_enable(RCU_GPIOC);
    rcu_periph_clock_enable(RCU_GPIOD);
    rcu_periph_clock_enable(RCU_GPIOE);

    /* configure OUT-H1, OUT-H2 GPIOE port */
    gpio_mode_set(GPIOE, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO_PIN_1 | GPIO_PIN_0);
    gpio_output_options_set(GPIOE, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_1 | GPIO_PIN_0);
    /* reset pin */
    gpio_bit_reset(GPIOE, GPIO_PIN_1 | GPIO_PIN_0);

    /* configure OUT-H3, OUT-H4, OUT-H5 GPIOB port */
    gpio_mode_set(GPIOB, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO_PIN_9 | GPIO_PIN_8 | GPIO_PIN_5);
    gpio_output_options_set(GPIOB, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_9 | GPIO_PIN_8 | GPIO_PIN_5);
    /* reset pin */
    gpio_bit_reset(GPIOB, GPIO_PIN_9 | GPIO_PIN_8 | GPIO_PIN_5);

    /* configure OUT-H6 ~ OUT-H12 GPIOD port */
    gpio_mode_set(GPIOD, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO_PIN_7 | GPIO_PIN_6 | GPIO_PIN_5 | GPIO_PIN_4 | GPIO_PIN_3 | GPIO_PIN_1 | GPIO_PIN_0);
    gpio_output_options_set(GPIOD, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_7 | GPIO_PIN_6 | GPIO_PIN_5 | GPIO_PIN_4 | GPIO_PIN_3 | GPIO_PIN_1 | GPIO_PIN_0);
    /* reset pin */
    gpio_bit_reset(GPIOD, GPIO_PIN_7 | GPIO_PIN_6 | GPIO_PIN_5 | GPIO_PIN_4 | GPIO_PIN_3 | GPIO_PIN_1 | GPIO_PIN_0);

    /* configure OUT-H13, OUT-H14 GPIOE port */
    gpio_mode_set(GPIOC, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO_PIN_11 | GPIO_PIN_10);
    gpio_output_options_set(GPIOC, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_11 | GPIO_PIN_10);
    /* reset pin */
    gpio_bit_reset(GPIOC, GPIO_PIN_11 | GPIO_PIN_10);

    /* configure OUT-H15, OUT-H16 GPIOE port */
    gpio_mode_set(GPIOA, GPIO_MODE_OUTPUT, GPIO_PUPD_NONE, GPIO_PIN_15 | GPIO_PIN_12);
    gpio_output_options_set(GPIOA, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_15 | GPIO_PIN_12);
    /* reset pin */
    gpio_bit_reset(GPIOA, GPIO_PIN_15 | GPIO_PIN_12);

    OUT_bit_write(0x0000U);
}

/*!
    \brief    set the value of pin bit of OUT port.
    \param[in]  pin: pin of  OUT port.
                one or more parameters can be selected which are shown as below:
      \arg        GPIO_PIN_x(x=0..15), GPIO_PIN_ALL
    \retval     none
*/
void OUT_bit_write(uint32_t pin)
{
    uint8_t i;
    uint32_t mask = 0;

    for( i = 0; i < 16; i++)
    {
        mask = pin & BIT(i);


        switch(i)
        {
            case 0: /* OUT_H1  <---> PE1 */
                if( mask )
                {
                    gpio_bit_set(GPIOE, GPIO_PIN_1);
                }
                else
                {
                    gpio_bit_reset(GPIOE, GPIO_PIN_1);
                }

                break;

            case 1: /* OUT_H2  <---> PE0 */
                if( mask )
                {
                    gpio_bit_set(GPIOE, GPIO_PIN_0);
                }
                else
                {
                    gpio_bit_reset(GPIOE, GPIO_PIN_0);
                }

                break;

            case 2: /* OUT_H3  <---> PB9 */
                if( mask )
                {
                    gpio_bit_set(GPIOB, GPIO_PIN_9);
                }
                else
                {
                    gpio_bit_reset(GPIOB, GPIO_PIN_9);
                }

                break;

            case 3: /* OUT_H4  <---> PB8 */
                if( mask )
                {
                    gpio_bit_set(GPIOB, GPIO_PIN_8);
                }
                else
                {
                    gpio_bit_reset(GPIOB, GPIO_PIN_8);
                }

                break;

            case 4: /* OUT_H5  <---> PB5 */
                if( mask )
                {
                    gpio_bit_set(GPIOB, GPIO_PIN_5);
                }
                else
                {
                    gpio_bit_reset(GPIOB, GPIO_PIN_5);
                }

                break;

            case 5: /* OUT_H6  <---> PD7 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_7);
                }
                else
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_7);
                }

                break;

            case 6: /* OUT_H7  <---> PD6 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_6);
                }
                else
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_6);
                }

                break;

            case 7: /* OUT_H8  <---> PD5 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_5);
                }
                else
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_5);
                }

                break;

            case 8: /* OUT_H9  <---> PD4 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_4);
                }
                else
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_4);
                }

                break;

            case 9: /* OUT_H10 <---> PD3 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_3);
                }
                else
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_3);
                }

                break;

            case 10: /* OUT_H11 <---> PD1 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_1);
                }
                else
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_1);
                }

                break;

            case 11: /* OUT_H12 <---> PD0 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_0);
                }
                else
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_0);
                }

                break;

            case 12: /* OUT_H13 <---> PC11 */
                if( mask )
                {
                    gpio_bit_set(GPIOC, GPIO_PIN_11);
                }
                else
                {
                    gpio_bit_reset(GPIOC, GPIO_PIN_11);
                }

                break;

            case 13: /* OUT_H14 <---> PC10 */
                if( mask )
                {
                    gpio_bit_set(GPIOC, GPIO_PIN_10);
                }
                else
                {
                    gpio_bit_reset(GPIOC, GPIO_PIN_10);
                }

                break;

            case 14: /* OUT_H15 <---> PA15 */
                if( mask )
                {
                    gpio_bit_set(GPIOA, GPIO_PIN_15);
                }
                else
                {
                    gpio_bit_reset(GPIOA, GPIO_PIN_15);
                }

                break;

            case 15: /* OUT_H16 <---> PA12 */
                if( mask )
                {
                    gpio_bit_set(GPIOA, GPIO_PIN_12);
                }
                else
                {
                    gpio_bit_reset(GPIOA, GPIO_PIN_12);
                }

                break;
        }
    }
}

/*!
    \brief    reset bit of the pin of OUT port.
    \param[in]  pin: pin of  OUT port.
                one or more parameters can be selected which are shown as below:
      \arg        GPIO_PIN_x(x=0..15), GPIO_PIN_ALL
    \retval     none
*/
void OUT_bit_reset(uint32_t pin)
{
    uint8_t i;
    uint32_t mask = 0;

    for( i = 0; i < 16; i++)
    {
        mask = pin & BIT(i);

        switch(i)
        {
            case 0: /* OUT_H1  <---> PE1 */
                if( mask )
                {
                    gpio_bit_reset(GPIOE, GPIO_PIN_1);
                }

                break;

            case 1: /* OUT_H2  <---> PE0 */
                if( mask )
                {
                    gpio_bit_reset(GPIOE, GPIO_PIN_0);
                }

                break;

            case 2: /* OUT_H3  <---> PB9 */
                if( mask )
                {
                    gpio_bit_reset(GPIOB, GPIO_PIN_9);
                }

                break;

            case 3: /* OUT_H4  <---> PB8 */
                if( mask )
                {
                    gpio_bit_reset(GPIOB, GPIO_PIN_8);
                }

                break;

            case 4: /* OUT_H5  <---> PB5 */
                if( mask )
                {
                    gpio_bit_reset(GPIOB, GPIO_PIN_5);
                }

                break;

            case 5: /* OUT_H6  <---> PD7 */
                if( mask )
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_7);
                }

                break;

            case 6: /* OUT_H7  <---> PD6 */
                if( mask )
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_6);
                }

                break;

            case 7: /* OUT_H8  <---> PD5 */
                if( mask )
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_5);
                }

                break;

            case 8: /* OUT_H9  <---> PD4 */
                if( mask )
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_4);
                }

                break;

            case 9: /* OUT_H10 <---> PD3 */
                if( mask )
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_3);
                }

                break;

            case 10: /* OUT_H11 <---> PD1 */
                if( mask )
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_1);
                }

                break;

            case 11: /* OUT_H12 <---> PD0 */
                if( mask )
                {
                    gpio_bit_reset(GPIOD, GPIO_PIN_0);
                }

                break;

            case 12: /* OUT_H13 <---> PC11 */
                if( mask )
                {
                    gpio_bit_reset(GPIOC, GPIO_PIN_11);
                }

                break;

            case 13: /* OUT_H14 <---> PC10 */
                if( mask )
                {
                    gpio_bit_reset(GPIOC, GPIO_PIN_10);
                }

                break;

            case 14: /* OUT_H15 <---> PA15 */
                if( mask )
                {
                    gpio_bit_reset(GPIOA, GPIO_PIN_15);
                }

                break;

            case 15: /* OUT_H16 <---> PA12 */
                if( mask )
                {
                    gpio_bit_reset(GPIOA, GPIO_PIN_12);
                }

                break;
        }
    }
}

/*!
    \brief    set bit of the pin of OUT port.
    \param[in]  pin: pin of  OUT port.
                one or more parameters can be selected which are shown as below:
      \arg        GPIO_PIN_x(x=0..15), GPIO_PIN_ALL
    \retval     none
*/
void OUT_bit_set(uint32_t pin)
{
    uint8_t i;
    uint32_t mask = 0;

    for( i = 0; i < 16; i++)
    {
        mask = pin & BIT(i);

        switch(i)
        {
            case 0: /* OUT_H1  <---> PE1 */
                if( mask )
                {
                    gpio_bit_set(GPIOE, GPIO_PIN_1);
                }

                break;

            case 1: /* OUT_H2  <---> PE0 */
                if( mask )
                {
                    gpio_bit_set(GPIOE, GPIO_PIN_0);
                }

                break;

            case 2: /* OUT_H3  <---> PB9 */
                if( mask )
                {
                    gpio_bit_set(GPIOB, GPIO_PIN_9);
                }

                break;

            case 3: /* OUT_H4  <---> PB8 */
                if( mask )
                {
                    gpio_bit_set(GPIOB, GPIO_PIN_8);
                }

                break;

            case 4: /* OUT_H5  <---> PB5 */
                if( mask )
                {
                    gpio_bit_set(GPIOB, GPIO_PIN_5);
                }

                break;

            case 5: /* OUT_H6  <---> PD7 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_7);
                }

                break;

            case 6: /* OUT_H7  <---> PD6 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_6);
                }

                break;

            case 7: /* OUT_H8  <---> PD5 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_5);
                }

                break;

            case 8: /* OUT_H9  <---> PD4 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_4);
                }

                break;

            case 9: /* OUT_H10 <---> PD3 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_3);
                }

                break;

            case 10: /* OUT_H11 <---> PD1 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_1);
                }

                break;

            case 11: /* OUT_H12 <---> PD0 */
                if( mask )
                {
                    gpio_bit_set(GPIOD, GPIO_PIN_0);
                }

                break;

            case 12: /* OUT_H13 <---> PC11 */
                if( mask )
                {
                    gpio_bit_set(GPIOC, GPIO_PIN_11);
                }

                break;

            case 13: /* OUT_H14 <---> PC10 */
                if( mask )
                {
                    gpio_bit_set(GPIOC, GPIO_PIN_10);
                }

                break;

            case 14: /* OUT_H15 <---> PA15 */
                if( mask )
                {
                    gpio_bit_set(GPIOA, GPIO_PIN_15);
                }

                break;

            case 15: /* OUT_H16 <---> PA12 */
                if( mask )
                {
                    gpio_bit_set(GPIOA, GPIO_PIN_12);
                }

                break;
        }
    }
}

/*!
    \brief    Test bit of OUT port throught Chaser Lights.
    \param[in]  ms: count in milliseconds
    \param[out] none
    \retval     none
*/
void OUT_chaser_lights(uint32_t ms)
{
    static uint32_t index = 0x1;
    /* turn on the followed LED of OUT port, urn off the previous LED of OUT port */
    OUT_bit_write(~index);
    delay_1ms(ms);

    if(index == 0x8000)
    {
        index = 0x1;
    }
    else
    {
        index <<= 0x1;
    }
}


