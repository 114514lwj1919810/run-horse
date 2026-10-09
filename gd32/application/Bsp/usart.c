/*!
    \file  usart.c
    \brief firmware functions to manage leds, keys, COM ports

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/

/*
    Copyright (c) 2026, GigaDevice Semiconductor Inc.

    Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    1. Redistributions of source code must retain the above copyright notice, this
       list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright notice,
       this list of conditions and the following disclaimer in the documentation
       and/or other materials provided with the distribution.
    3. Neither the name of the copyright holder nor the names of its contributors
       may be used to endorse or promote products derived from this software without
       specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
OF SUCH DAMAGE.
*/

#include "usart.h"
#include <stdio.h>

int fputc(int ch, FILE *f)
{
    usart_data_transmit(USART0, (uint8_t)ch);

    while(RESET == usart_flag_get(USART0, USART_FLAG_TBE));

    return ch;
}

/*!
    \brief      USART transmit a byte of data
    \param[in]  usart_periph: USARTx(x=0,1,2,5)/UARTx(x=3,4,6,7)
    \param[in]  data: data of transmission
    \param[out] none
    \retval     none
*/
void USART_send_byte( uint32_t usart_periph, uint8_t data)
{
    /* Send a byte of data to USART */
    usart_data_transmit(usart_periph, data);

    /* waiting for transmiting data over */
    while (usart_flag_get(usart_periph, USART_FLAG_TBE) == RESET);
}

/*!
    \brief      USART transmit several bytes of data
    \param[in]  usart_periph: USARTx(x=0,1,2,5)/UARTx(x=3,4,6,7)
    \param[in]  data: data of transmission
    \param[in]  length: byte counts of transmission data
    \param[out] none
    \retval     none
*/
void USART_send_bytes( uint32_t usart_periph, uint8_t *data, uint8_t length)
{
    while(length--)
    {
        USART_send_byte(usart_periph, *data++);
    }
}

/*!
    \brief    USART receive a byte of data
    \param[in]  usart_periph: USARTx(x=0,1,2,5)/UARTx(x=3,4,6,7)
    \param[out] none
    \retval     data of received
*/
uint8_t USART_receive_byte(uint32_t usart_periph)
{
    while (usart_flag_get(usart_periph, USART_FLAG_RBNE) == RESET);

    return usart_data_receive(usart_periph);
}

// 接收一个字节数据
uint8_t USART0_receive_byte(void)
{
    while (usart_flag_get(USART0, USART_FLAG_RBNE) == RESET); // 等待接收完成

    return usart_data_receive(USART0);
}


/*!
    \brief      configure USART0 port
    \param[in]  none
    \param[out] none
    \retval     none
*/
void USART0_init(void)
{
    /* enable GPIOA clock */
    rcu_periph_clock_enable(RCU_GPIOA);

    /* enable USART0 clock */
    rcu_periph_clock_enable(RCU_USART0);

    /* connect port to USART0_Tx */
    gpio_af_set(GPIOA, GPIO_AF_7, GPIO_PIN_9);

    /* connect port to USART0_Rx */
    gpio_af_set(GPIOA, GPIO_AF_7, GPIO_PIN_10);

    /* configure USART Tx as alternate function push-pull */
    gpio_mode_set(GPIOA, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_9);
    gpio_output_options_set(GPIOA, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_9);

    /* configure USART Rx as alternate function push-pull */
    gpio_mode_set(GPIOA, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_10);
    gpio_output_options_set(GPIOA, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_10);

    /* USART configure */
    usart_deinit(USART0);                                  // 复位串口
    usart_baudrate_set(USART0, 115200U);                   // 设置波特率
    usart_parity_config(USART0, USART_PM_NONE);            // 无校验
    usart_word_length_set(USART0, USART_WL_8BIT);          // 8位数据位
    usart_stop_bit_set(USART0, USART_STB_1BIT);            // 1位停止位
    usart_receive_config(USART0, USART_RECEIVE_ENABLE);    // 使能串口接收
    usart_transmit_config(USART0, USART_TRANSMIT_ENABLE);  // 使能串口发送
    usart_enable(USART0);                                  // 使能串口

//    usart_interrupt_enable(USART0,USART_INT_RBNE);         // 使能串口中断
//    nvic_priority_group_set(NVIC_PRIGROUP_PRE2_SUB2);      // 中断等级分组
//    nvic_irq_enable(USART0_IRQn,1,1);                      // 设置串口中断等级
}



/*!
    \brief      configure USART1 port, FX1 9600baud
    \param[in]  none
    \param[out] none
    \retval     none
*/
void USART1_init(void)
{
    /* enable GPIOA clock */
    rcu_periph_clock_enable(RCU_GPIOA);

    /* enable USART1 clock */
    rcu_periph_clock_enable(RCU_USART1);

    /* connect port to USART0_Tx */
    gpio_af_set(GPIOA, GPIO_AF_7, GPIO_PIN_2);

    /* connect port to USART0_Rx */
    gpio_af_set(GPIOA, GPIO_AF_7, GPIO_PIN_3);

    /* configure USART Tx as alternate function push-pull */
    gpio_mode_set(GPIOA, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_2);
    gpio_output_options_set(GPIOA, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_2);

    /* configure USART Rx as alternate function push-pull */
    gpio_mode_set(GPIOA, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_3);
    gpio_output_options_set(GPIOA, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_3);

    /* USART configure */
    usart_deinit(USART1);                                  // 复位串口
    usart_baudrate_set(USART1, 9600U);                   // 设置波特率
    usart_parity_config(USART1, USART_PM_NONE);            // 无校验
    usart_word_length_set(USART1, USART_WL_8BIT);          // 8位数据位
    usart_stop_bit_set(USART1, USART_STB_1BIT);            // 1位停止位
    usart_receive_config(USART1, USART_RECEIVE_ENABLE);    // 使能串口接收
    usart_transmit_config(USART1, USART_TRANSMIT_ENABLE);  // 使能串口发送
    usart_enable(USART1);                                  // 使能串口

//    usart_interrupt_enable(USART1,USART_INT_RBNE);         // 使能串口中断
//    nvic_priority_group_set(NVIC_PRIGROUP_PRE2_SUB2);      // 中断等级分组
//    nvic_irq_enable(USART1_IRQn,1,1);                      // 设置串口中断等级
}

/*!
    \brief      configure USART2 port
    \param[in]  none
    \param[out] none
    \retval     none
*/
void USART2_init(void)
{
    /* enable GPIOA clock */
    rcu_periph_clock_enable(RCU_GPIOB);

    /* enable USART2 clock */
    rcu_periph_clock_enable(RCU_USART2);

    /* connect port to USART0_Tx */
    gpio_af_set(GPIOB, GPIO_AF_7, GPIO_PIN_10);

    /* connect port to USART0_Rx */
    gpio_af_set(GPIOB, GPIO_AF_7, GPIO_PIN_11);

    /* configure USART Tx as alternate function push-pull */
    gpio_mode_set(GPIOB, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_10);
    gpio_output_options_set(GPIOB, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_10);

    /* configure USART Rx as alternate function push-pull */
    gpio_mode_set(GPIOB, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_11);
    gpio_output_options_set(GPIOB, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_11);

    /* USART configure */
    usart_deinit(USART2);                                  // 复位串口
    usart_baudrate_set(USART2, 115200U);                   // 设置波特率
    usart_parity_config(USART2, USART_PM_NONE);            // 无校验
    usart_word_length_set(USART2, USART_WL_8BIT);          // 8位数据位
    usart_stop_bit_set(USART2, USART_STB_1BIT);            // 1位停止位
    usart_receive_config(USART2, USART_RECEIVE_ENABLE);    // 使能串口接收
    usart_transmit_config(USART2, USART_TRANSMIT_ENABLE);  // 使能串口发送
    usart_enable(USART2);                                  // 使能串口

//    usart_interrupt_enable(USART2,USART_INT_RBNE);         // 使能串口中断
//    nvic_priority_group_set(NVIC_PRIGROUP_PRE2_SUB2);      // 中断等级分组
//    nvic_irq_enable(USART2_IRQn,1,1);                      // 设置串口中断等级
}

/*!
    \brief      configure UART3 port, , FX2 9600baud
    \param[in]  none
    \param[out] none
    \retval     none
*/
void UART3_init(void)
{
    /* enable GPIOA clock */
    rcu_periph_clock_enable(RCU_GPIOA);

    /* enable UART3 clock */
    rcu_periph_clock_enable(RCU_UART3);

    /* connect port to USART0_Tx */
    gpio_af_set(GPIOA, GPIO_AF_8, GPIO_PIN_0);

    /* connect port to USART0_Rx */
    gpio_af_set(GPIOA, GPIO_AF_8, GPIO_PIN_1);

    /* configure UART Tx as alternate function push-pull */
    gpio_mode_set(GPIOA, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_0);
    gpio_output_options_set(GPIOA, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_0);

    /* configure UART Rx as alternate function push-pull */
    gpio_mode_set(GPIOA, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_1);
    gpio_output_options_set(GPIOA, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_1);

    /* USART configure */
    usart_deinit(UART3);                                  // 复位串口
    usart_baudrate_set(UART3, 9600U);                   // 设置波特率
    usart_parity_config(UART3, USART_PM_NONE);            // 无校验
    usart_word_length_set(UART3, USART_WL_8BIT);          // 8位数据位
    usart_stop_bit_set(UART3, USART_STB_1BIT);            // 1位停止位
    usart_receive_config(UART3, USART_RECEIVE_ENABLE);    // 使能串口接收
    usart_transmit_config(UART3, USART_TRANSMIT_ENABLE);  // 使能串口发送
    usart_enable(UART3);                                  // 使能串口

//    usart_interrupt_enable(UART3,USART_INT_RBNE);         // 使能串口中断
//    nvic_priority_group_set(NVIC_PRIGROUP_PRE2_SUB2);      // 中断等级分组
//    nvic_irq_enable(UART3_IRQn,1,1);                      // 设置串口中断等级
}

/*!
    \brief      configure UART4 port
    \param[in]  none
    \param[out] none
    \retval     none
*/
void UART4_init(void)
{
    /* enable GPIOA clock */
    rcu_periph_clock_enable(RCU_GPIOC);
    rcu_periph_clock_enable(RCU_GPIOD);

    /* enable UART3 clock */
    rcu_periph_clock_enable(RCU_UART4);

    /* connect port to USART0_Tx */
    gpio_af_set(GPIOC, GPIO_AF_8, GPIO_PIN_12);

    /* connect port to USART0_Rx */
    gpio_af_set(GPIOD, GPIO_AF_8, GPIO_PIN_2);

    /* configure UART Tx as alternate function push-pull */
    gpio_mode_set(GPIOC, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_12);
    gpio_output_options_set(GPIOC, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_12);

    /* configure UART Rx as alternate function push-pull */
    gpio_mode_set(GPIOD, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_2);
    gpio_output_options_set(GPIOD, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_2);

    /* USART configure */
    usart_deinit(UART4);                                  // 复位串口
    usart_baudrate_set(UART4, 115200U);                   // 设置波特率
    usart_parity_config(UART4, USART_PM_NONE);            // 无校验
    usart_word_length_set(UART4, USART_WL_8BIT);          // 8位数据位
    usart_stop_bit_set(UART4, USART_STB_1BIT);            // 1位停止位
    usart_receive_config(UART4, USART_RECEIVE_ENABLE);    // 使能串口接收
    usart_transmit_config(UART4, USART_TRANSMIT_ENABLE);  // 使能串口发送
    usart_enable(UART4);                                  // 使能串口

//    usart_interrupt_enable(UART4,USART_INT_RBNE);         // 使能串口中断
//    nvic_priority_group_set(NVIC_PRIGROUP_PRE2_SUB2);      // 中断等级分组
//    nvic_irq_enable(UART4_IRQn,1,1);                      // 设置串口中断等级
}

/*!
    \brief      configure USART5 port
    \param[in]  none
    \param[out] none
    \retval     none
*/
void USART5_init(void)
{
    /* enable GPIOA clock */
    rcu_periph_clock_enable(RCU_GPIOC);

    /* enable USART2 clock */
    rcu_periph_clock_enable(RCU_USART5);

    /* connect port to USART0_Tx */
    gpio_af_set(GPIOC, GPIO_AF_8, GPIO_PIN_6);

    /* connect port to USART0_Rx */
    gpio_af_set(GPIOC, GPIO_AF_8, GPIO_PIN_7);

    /* configure USART Tx as alternate function push-pull */
    gpio_mode_set(GPIOC, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_6);
    gpio_output_options_set(GPIOC, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_6);

    /* configure USART Rx as alternate function push-pull */
    gpio_mode_set(GPIOC, GPIO_MODE_AF, GPIO_PUPD_PULLUP, GPIO_PIN_7);
    gpio_output_options_set(GPIOC, GPIO_OTYPE_PP, GPIO_OSPEED_50MHZ, GPIO_PIN_7);

    /* USART configure */
    usart_deinit(USART5);                                  // 复位串口
    usart_baudrate_set(USART5, 115200U);                   // 设置波特率
    usart_parity_config(USART5, USART_PM_NONE);            // 无校验
    usart_word_length_set(USART5, USART_WL_8BIT);          // 8位数据位
    usart_stop_bit_set(USART5, USART_STB_1BIT);            // 1位停止位
    usart_receive_config(USART5, USART_RECEIVE_ENABLE);    // 使能串口接收
    usart_transmit_config(USART5, USART_TRANSMIT_ENABLE);  // 使能串口发送
    usart_enable(USART5);                                  // 使能串口

//    usart_interrupt_enable(USART5,USART_INT_RBNE);         // 使能串口中断
//    nvic_priority_group_set(NVIC_PRIGROUP_PRE2_SUB2);      // 中断等级分组
//    nvic_irq_enable(USART5_IRQn,1,1);                      // 设置串口中断等级
}
