#ifndef __AUDIO_H
#define __AUDIO_H

#include <string.h>
#include <stdint.h>
#include "usart.h"
#include "systick.h"

void DoSum( uint8_t *Str, uint8_t len);
void USART_send_cmd(uint32_t usart_periph, uint8_t cmd, uint8_t feedback, uint16_t data);
void audio_backgroud_test(void);
void audio_game_test(void);

/*
 * 双KT404C连接：
 * - 音效板：UART4_TX/PA0发送，UART4_RX/PA1接收播放完成回包。
 * - 第一条触发音效播放根目录物理序号，后续音效从ADVERT1执行单层插播。
 *
 * - 背景板：USART3_TX/PB10，循环播放背景音乐。
 * - 两路均为9600波特率、8位数据、无校验、1位停止位。
 * - 插播期间的新音效进入FIFO，当前插播结束后继续发送，不做嵌套插播。
 */

#define FX1_USART  USART1
#define FX2_USART  UART3

/* FX1-TX <---> PA2  <---> USART1_TX (AF7) */
/* FX1-RX <---> PA3  <---> USART1_RX (AF7) */


/* FX2-TX <---> PA0  <---> UART3_TX (AF8)  */
/* FX2-RX <---> PA1  <---> UART3_RX (AF8)  */

typedef enum
{
    AUDIO_CHIP_1 = 0,
    AUDIO_CHIP_2,
    AUDIO_CHIP_COUNT
} Audio_Chip;

typedef enum
{
    AUDIO_STATE_IDLE = 0,
    AUDIO_STATE_WAIT,
    AUDIO_STATE_STARTUP_WAIT,
    AUDIO_STATE_ERROR
} Audio_State;

/* 背景音乐板开关：0=完全屏蔽背景板，1=背景音乐和触发音效同时启用。 */
extern uint8_t g_background_music_enable;

void Audio_Init(void);
void Audio_Task(void);

void Audio_PlayIndexOn(Audio_Chip chip, uint16_t index);
void Audio_LoopIndexOn(Audio_Chip chip, uint16_t index);
void Audio_CancelLoopResumeOn(Audio_Chip chip);
void Audio_StopOn(Audio_Chip chip);

/* E15专用独占报警，开始时停两块板并循环index，结束时停止报警。 */
void Audio_StartExclusiveAlarm(uint16_t index);
void Audio_StopExclusiveAlarm(void);

void Audio_VolumeUpOn(Audio_Chip chip);
void Audio_VolumeDownOn(Audio_Chip chip);
void Audio_SetVolumeLevelOn(Audio_Chip chip, uint8_t level);
void Audio_SetVolumeLevel(uint8_t level);

void Audio_SendHexOn(Audio_Chip chip, const uint8_t *hex, uint16_t len);
uint8_t Audio_SendHexStringOn(Audio_Chip chip, const char *hex_string);

Audio_State Audio_GetStateOn(Audio_Chip chip);

#endif
