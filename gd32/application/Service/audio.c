#include "audio.h"
#include <stdio.h>

/******************************************************************************
 - 功能描述： 串口向外发送命令[包括控制和查询]
 - 参数说明： CMD:表示控制指令，请查阅指令表，还包括查询的相关指令
              feedback:是否需要应答[0:不需要应答，1:需要应答]
              data:传送的参数
******************************************************************************/
void USART_send_cmd(uint32_t usart_periph, uint8_t cmd, uint8_t feedback, uint16_t data)
{
    uint8_t Send_buf[8] = {0} ;
		uint16_t xorsum = 0;
		uint8_t i;
		
		/* construct data payload of command frame */	
    Send_buf[0] = 0xff;                  // 地址信息--默认为0xFF
    Send_buf[1] = 0x06;                  // 数据负载长度，包括地址信息、长度、控制指令、反馈和数据
    Send_buf[2] = cmd;                   // 控制指令
    Send_buf[3] = feedback;              // 是否需要反馈
    Send_buf[4] = (uint8_t)(data >> 8);  // 数据高位
    Send_buf[5] = (uint8_t)(data);       // 数据低位		
       
    for(i = 0; i < 6; i++)               // 计算校验位    
    {
        xorsum  = xorsum + Send_buf[i];
    }

    xorsum = 0 - xorsum;
    Send_buf[6] = (uint8_t)(xorsum >> 8);
    Send_buf[7] = (uint8_t)(xorsum & 0x00ff);

    /* Send the constructed command frame */
    USART_send_byte(usart_periph, 0x7E);   // 起始位0x7E

    for(i = 0; i < 8; i++)                 // 命令帧数据
    {
        USART_send_byte(usart_periph, Send_buf[i]);
    }

    USART_send_byte(usart_periph, 0xEF) ;  // 结束位0xEF
}

void audio_backgroud_test(void)
{    
    UART3_init();   //USRT3-FX2串口初始化
    USART_send_cmd(UART3, 0x03, 0, 0x01) ;  //播放第一首
    delay_1ms(6000) ;//延时大概6S
    printf("play one\r\n");

    USART_send_cmd(UART3, 0x03, 0, 0x02) ;  //播放第二首
    delay_1ms(6000) ;//延时大概6S
    printf("play two\r\n");

    USART_send_cmd(UART3, 0x03, 0, 0x04) ;  //播放第四首
    printf("play three\r\n");
}

void audio_game_test(void)
{
    USART1_init();  //USART1-FX1串口初始化   
    USART_send_cmd(USART1, 0x03, 0, 0x01) ;  //播放第一首
    delay_1ms(6000) ;//延时大概6S
    printf("play one\r\n");

    USART_send_cmd(USART1, 0x03, 0, 0x02) ;  //播放第二首
    delay_1ms(6000) ;//延时大概6S
    printf("play two\r\n");

    USART_send_cmd(USART1, 0x03, 0, 0x04) ;  //播放第四首
    printf("play three\r\n");
}

///*
// * 本文件按已经过实机验证的 joker-music 音频驱动工作：
// * - 发送 KT404C 兼容的 8 字节无校验帧，并接收音效板的播放完成回包。
// * - 第一条普通音效使用根目录物理序号0x03，后续音效使用ADVERT1的0x25插播。
// * - KT404C只支持一层插播；插播期间的新音效进入FIFO，当前插播结束后再发送。
// * - 单曲循环使用 0x08，停止使用 0x16。
// * - 两条命令之间固定间隔 120ms，保持与 joker-music 一致。
// */
//#define AUDIO_UART_TIMEOUT_MS           20U
//#define AUDIO_COMMAND_GAP_MS            120U
//#define AUDIO_STARTUP_DELAY_MS          1500U
//#define AUDIO_STARTUP_VOLUME_REPEAT     2U
//#define AUDIO_FRAME_LEN                 8U
//#define AUDIO_TX_BUF_LEN                32U
//#define AUDIO_QUEUE_DEPTH               8U
//#define AUDIO_INIT_VOLUME               15U
//#define AUDIO_MODULE_MAX_VOLUME         30U
//#define AUDIO_EFFECT_ADVERT_FOLDER      1U
//#define AUDIO_EFFECT_RX_FRAME_LEN       10U
//#define AUDIO_EFFECT_COMPLETE_MIN_MS    120U
//#define AUDIO_EFFECT_COMPLETE_MARGIN_MS 120U
//#define AUDIO_EFFECT_DURATION_GUARD_MS  250U
//#define AUDIO_EFFECT_DEFAULT_MS         5000U
//#define AUDIO_EFFECT_AUDIBLE_GUARD_MS   20U

//typedef struct
//{
//    Audio_State state;

//    uint8_t queue_data[AUDIO_QUEUE_DEPTH][AUDIO_TX_BUF_LEN];
//    uint16_t queue_length[AUDIO_QUEUE_DEPTH];
//    uint8_t queue_head;
//    uint8_t queue_tail;
//    uint8_t queue_count;

//    uint8_t target_volume;
//    uint8_t startup_volume_frames;
//    uint32_t state_tick;
//} Audio_Context;

//typedef struct
//{
//    uint16_t index;
//    uint8_t loop;
//} Audio_EffectEvent;

//typedef struct
//{
//    Audio_EffectEvent pending_event;
//    uint8_t pending_valid;

//    uint8_t protected_loop_active;
//    uint16_t protected_loop_index;
//    uint8_t protected_resume_needed;

//    uint8_t transient_active;
//    uint16_t transient_index;
//    uint32_t transient_tick;
//    uint32_t transient_audible_ms;

//    uint8_t base_active;
//    uint8_t base_loop;
//    uint8_t advert_active;
//    uint16_t base_index;
//    uint16_t advert_index;
//    uint32_t base_remaining_ms;
//    uint32_t base_tick;
//    uint32_t advert_tick;
//    uint32_t advert_min_complete_ms;
//    uint32_t advert_timeout_ms;

//    uint8_t rx_frame[AUDIO_EFFECT_RX_FRAME_LEN];
//    uint8_t rx_length;
//} Audio_EffectContext;

///* 根目录和ADVERT1中1~19号音效的实测时长，超时仅作为串口回包丢失时的保护。 */
//static const uint16_t s_audio_effect_duration_ms[] =
//{
//    2196U, 1565U, 932U, 524U,
//    1231U, 1103U, 860U, 2744U,
//    266U, 1325U, 13282U, 3989U,
//    322U, 1051U, 1061U, 322U,
//    2280U, 3552U, 3744U
//};

///*
// * ADVERT1文件从开始到“最后可听声音”的实测时间。部分MP3尾部有0.3~1.9秒
// * 静音，若等待完整文件结束，持续抽奖音效013听起来就会恢复过慢。
// */
//static const uint16_t s_audio_effect_audible_end_ms[] =
//{
//    2107U, 1240U, 841U, 524U,
//    1128U, 641U, 860U, 2523U,
//    266U, 1267U, 11413U, 3989U,
//    322U, 1051U, 1061U, 322U,
//    1412U, 2704U, 2891U
//};

//static Audio_Context s_audio_ctx[AUDIO_CHIP_COUNT];
//static Audio_EffectContext s_effect_ctx;
//static uint8_t s_audio_initialized;
//static uint8_t s_exclusive_alarm_active;

///* 0：完全屏蔽背景音乐板；1：音效板和背景音乐板同时工作。 */
//uint8_t g_background_music_enable = 1U;

//static uint8_t Audio_IsElapsed(uint32_t start, uint32_t period)
//{
//    return ((uint32_t)(HAL_GetTick() - start) >= period) ? 1U : 0U;
//}

//static uint8_t Audio_IsValidChip(Audio_Chip chip)
//{
//    return (chip < AUDIO_CHIP_COUNT) ? 1U : 0U;
//}

//static UART_HandleTypeDef *Audio_GetHandle(Audio_Chip chip)
//{
//    return (chip == AUDIO_CHIP_2) ?
//           &g_audio_uart2_handle : &g_audio_uart1_handle;
//}

///* 构造与 joker-music 完全一致的 KT404C 8 字节无校验控制帧。 */
//static void Audio_BuildFrame(uint8_t *frame, uint8_t command, uint16_t value)
//{
//    frame[0] = 0x7EU;
//    frame[1] = 0xFFU;
//    frame[2] = 0x06U;
//    frame[3] = command;
//    frame[4] = 0x00U;
//    frame[5] = (uint8_t)(value >> 8U);
//    frame[6] = (uint8_t)value;
//    frame[7] = 0xEFU;
//}

//static void Audio_QueueClear(Audio_Context *ctx)
//{
//    if (ctx == 0)
//    {
//        return;
//    }

//    memset(ctx->queue_data, 0, sizeof(ctx->queue_data));
//    memset(ctx->queue_length, 0, sizeof(ctx->queue_length));
//    ctx->queue_head = 0U;
//    ctx->queue_tail = 0U;
//    ctx->queue_count = 0U;
//}

///* 队列满时沿用 joker-music 的处理：丢弃最旧命令，保留刚发生的新命令。 */
//static void Audio_QueuePush(Audio_Context *ctx,
//                            const uint8_t *data,
//                            uint16_t length)
//{
//    uint8_t tail;

//    if ((ctx == 0) || (data == 0) || (length == 0U) ||
//            (length > AUDIO_TX_BUF_LEN))
//    {
//        return;
//    }

//    if (ctx->queue_count >= AUDIO_QUEUE_DEPTH)
//    {
//        ctx->queue_head++;

//        if (ctx->queue_head >= AUDIO_QUEUE_DEPTH)
//        {
//            ctx->queue_head = 0U;
//        }

//        ctx->queue_count--;
//    }

//    tail = ctx->queue_tail;
//    memcpy(ctx->queue_data[tail], data, length);
//    ctx->queue_length[tail] = length;
//    ctx->queue_tail++;

//    if (ctx->queue_tail >= AUDIO_QUEUE_DEPTH)
//    {
//        ctx->queue_tail = 0U;
//    }

//    ctx->queue_count++;
//}

//static uint8_t Audio_QueuePop(Audio_Context *ctx,
//                              uint8_t *data,
//                              uint16_t *length)
//{
//    uint8_t head;

//    if ((ctx == 0) || (data == 0) || (length == 0) ||
//            (ctx->queue_count == 0U))
//    {
//        return 0U;
//    }

//    head = ctx->queue_head;
//    *length = ctx->queue_length[head];
//    memcpy(data, ctx->queue_data[head], *length);
//    ctx->queue_length[head] = 0U;

//    ctx->queue_head++;

//    if (ctx->queue_head >= AUDIO_QUEUE_DEPTH)
//    {
//        ctx->queue_head = 0U;
//    }

//    ctx->queue_count--;
//    return 1U;
//}

//static void Audio_EffectReset(void)
//{
//    memset(&s_effect_ctx, 0, sizeof(s_effect_ctx));
//    audio_usart_clear_rx(AUDIO_UART_CHANNEL_1);
//}

///*
// * 触发音效只保留最新请求。玩家连续投币或按键时，新事件直接覆盖尚未发送的
// * 旧事件，停止操作后不会再补播一长串历史声音。
// */
//static void Audio_EffectSetPending(uint16_t index, uint8_t loop)
//{
//    if (index == 0U)
//    {
//        return;
//    }

//    s_effect_ctx.pending_event.index = index;
//    s_effect_ctx.pending_event.loop = (loop != 0U) ? 1U : 0U;
//    s_effect_ctx.pending_valid = 1U;
//}

//static uint8_t Audio_EffectGetPending(Audio_EffectEvent *event)
//{
//    if ((event == 0) || (s_effect_ctx.pending_valid == 0U))
//    {
//        return 0U;
//    }

//    *event = s_effect_ctx.pending_event;
//    return 1U;
//}

//static void Audio_EffectClearPending(void)
//{
//    memset(&s_effect_ctx.pending_event, 0,
//           sizeof(s_effect_ctx.pending_event));
//    s_effect_ctx.pending_valid = 0U;
//}

//static uint32_t Audio_EffectGetRawDurationMs(uint16_t index)
//{
//    uint16_t count;

//    count = (uint16_t)(sizeof(s_audio_effect_duration_ms) /
//                       sizeof(s_audio_effect_duration_ms[0]));

//    if ((index >= 1U) && (index <= count))
//    {
//        return (uint32_t)s_audio_effect_duration_ms[index - 1U];
//    }

//    return AUDIO_EFFECT_DEFAULT_MS;
//}

//static uint32_t Audio_EffectGetDurationMs(uint16_t index)
//{
//    return Audio_EffectGetRawDurationMs(index) +
//           AUDIO_EFFECT_DURATION_GUARD_MS;
//}

//static uint32_t Audio_EffectGetAudibleMs(uint16_t index)
//{
//    uint16_t count;

//    count = (uint16_t)(sizeof(s_audio_effect_audible_end_ms) /
//                       sizeof(s_audio_effect_audible_end_ms[0]));

//    if ((index >= 1U) && (index <= count))
//    {
//        return (uint32_t)s_audio_effect_audible_end_ms[index - 1U] +
//               AUDIO_EFFECT_AUDIBLE_GUARD_MS;
//    }

//    return Audio_EffectGetRawDurationMs(index);
//}

///*
// * 完成回包不带可直接用于识别ADVERT1逻辑序号的信息。A暂停后迟到的完成回包
// * 可能出现在B已经开始播放之后，因此不能只用固定120ms判断B已结束。这里按TF卡
// * 中每个B的实测时长建立最早完成门限，并预留120ms解码误差。
// */
//static uint32_t Audio_EffectGetMinCompleteMs(uint16_t index)
//{
//    uint32_t duration;

//    duration = Audio_EffectGetRawDurationMs(index);

//    if (duration > (AUDIO_EFFECT_COMPLETE_MIN_MS +
//                    AUDIO_EFFECT_COMPLETE_MARGIN_MS))
//    {
//        return duration - AUDIO_EFFECT_COMPLETE_MARGIN_MS;
//    }

//    return AUDIO_EFFECT_COMPLETE_MIN_MS;
//}

//static void Audio_EffectClearBase(void)
//{
//    s_effect_ctx.base_active = 0U;
//    s_effect_ctx.base_loop = 0U;
//    s_effect_ctx.base_index = 0U;
//    s_effect_ctx.base_remaining_ms = 0U;
//    s_effect_ctx.base_tick = 0U;
//}

//static void Audio_EffectPauseBase(uint32_t now)
//{
//    uint32_t elapsed;

//    if ((s_effect_ctx.base_active == 0U) ||
//            (s_effect_ctx.base_loop != 0U) ||
//            (s_effect_ctx.advert_active != 0U))
//    {
//        return;
//    }

//    elapsed = (uint32_t)(now - s_effect_ctx.base_tick);

//    if (elapsed >= s_effect_ctx.base_remaining_ms)
//    {
//        Audio_EffectClearBase();
//        return;
//    }

//    s_effect_ctx.base_remaining_ms -= elapsed;
//    s_effect_ctx.base_tick = now;
//}

//static void Audio_EffectStartBase(uint16_t index, uint8_t loop, uint32_t now)
//{
//    s_effect_ctx.base_active = 1U;
//    s_effect_ctx.base_loop = (loop != 0U) ? 1U : 0U;
//    s_effect_ctx.base_index = index;
//    s_effect_ctx.base_remaining_ms = Audio_EffectGetDurationMs(index);
//    s_effect_ctx.base_tick = now;
//    s_effect_ctx.advert_active = 0U;
//    s_effect_ctx.advert_index = 0U;
//    s_effect_ctx.advert_min_complete_ms = 0U;
//    s_effect_ctx.advert_timeout_ms = 0U;
//}

//static void Audio_EffectStartAdvert(uint16_t index, uint32_t now)
//{
//    Audio_EffectPauseBase(now);
//    s_effect_ctx.advert_active = 1U;
//    s_effect_ctx.advert_index = index;
//    s_effect_ctx.advert_tick = now;
//    s_effect_ctx.advert_min_complete_ms =
//        Audio_EffectGetMinCompleteMs(index);
//    s_effect_ctx.advert_timeout_ms = Audio_EffectGetDurationMs(index);
//}

//static void Audio_EffectFinishAdvert(uint32_t now)
//{
//    (void)now;
//    s_effect_ctx.advert_active = 0U;
//    s_effect_ctx.advert_index = 0U;
//    s_effect_ctx.advert_tick = 0U;
//    s_effect_ctx.advert_min_complete_ms = 0U;
//    s_effect_ctx.advert_timeout_ms = 0U;
//    s_effect_ctx.transient_active = 0U;
//}

//static void Audio_EffectRetryAdvertAsBase(uint32_t now)
//{
//    uint16_t index;

//    (void)now;
//    index = s_effect_ctx.advert_index;
//    s_effect_ctx.advert_active = 0U;
//    s_effect_ctx.advert_index = 0U;
//    s_effect_ctx.advert_tick = 0U;
//    s_effect_ctx.advert_min_complete_ms = 0U;
//    s_effect_ctx.advert_timeout_ms = 0U;
//    s_effect_ctx.transient_active = 0U;

//    /* 已有更新请求时保留最新请求；否则把插播失败的B改为普通根目录播放。 */
//    if (s_effect_ctx.pending_valid == 0U)
//    {
//        Audio_EffectSetPending(index, 0U);
//    }
//}

//static void Audio_EffectHandleComplete(uint32_t now)
//{
//    uint32_t elapsed;

//    if (s_exclusive_alarm_active != 0U)
//    {
//        return;
//    }

//    if (s_effect_ctx.advert_active != 0U)
//    {
//        elapsed = (uint32_t)(now - s_effect_ctx.advert_tick);

//        if (elapsed < s_effect_ctx.advert_min_complete_ms)
//        {
//            /* 这是基础音效A或更早音效的迟到回包，B仍在播放。 */
//            return;
//        }

//        Audio_EffectFinishAdvert(now);
//        return;
//    }

//    if (s_effect_ctx.transient_active != 0U)
//    {
//        elapsed = (uint32_t)(now - s_effect_ctx.transient_tick);

//        if (elapsed < s_effect_ctx.transient_audible_ms)
//        {
//            return;
//        }

//        s_effect_ctx.transient_active = 0U;

//        if ((s_effect_ctx.base_active != 0U) &&
//                (s_effect_ctx.base_loop == 0U))
//        {
//            Audio_EffectClearBase();
//        }
//    }
//}

//static void Audio_ProcessRxByte(uint8_t value)
//{
//    uint8_t command;

//    if (s_effect_ctx.rx_length == 0U)
//    {
//        if (value == 0x7EU)
//        {
//            s_effect_ctx.rx_frame[0] = value;
//            s_effect_ctx.rx_length = 1U;
//        }

//        return;
//    }

//    if (s_effect_ctx.rx_length >= AUDIO_EFFECT_RX_FRAME_LEN)
//    {
//        s_effect_ctx.rx_length = 0U;
//        return;
//    }

//    s_effect_ctx.rx_frame[s_effect_ctx.rx_length++] = value;

//    if (s_effect_ctx.rx_length < AUDIO_EFFECT_RX_FRAME_LEN)
//    {
//        return;
//    }

//    if ((s_effect_ctx.rx_frame[0] == 0x7EU) &&
//            (s_effect_ctx.rx_frame[2] == 0x06U) &&
//            (s_effect_ctx.rx_frame[AUDIO_EFFECT_RX_FRAME_LEN - 1U] == 0xEFU))
//    {
//        command = s_effect_ctx.rx_frame[3];

//        if ((command == 0x3CU) || (command == 0x3DU) ||
//                (command == 0x3EU))
//        {
//            Audio_EffectHandleComplete(HAL_GetTick());
//        }
//        else if ((command == 0x40U) &&
//                 (s_effect_ctx.rx_frame[6] == 0x07U) &&
//                 (s_effect_ctx.advert_active != 0U))
//        {
//            Audio_EffectRetryAdvertAsBase(HAL_GetTick());
//        }
//    }

//    s_effect_ctx.rx_length = 0U;
//}

//static void Audio_ProcessRx(void)
//{
//    uint8_t data[16];
//    uint16_t count;
//    uint16_t index;

//    do
//    {
//        count = audio_usart_read(AUDIO_UART_CHANNEL_1,
//                                 data,
//                                 (uint16_t)sizeof(data));

//        for (index = 0U; index < count; index++)
//        {
//            Audio_ProcessRxByte(data[index]);
//        }
//    }
//    while (count == (uint16_t)sizeof(data));
//}

///*
// * 和 joker-music 一样使用阻塞发送。8 字节在 9600 波特率下约占 8.3ms，
// * 发送结束后才启动 120ms 命令保护间隔，避免模块内部上一条命令被覆盖。
// */
//static uint8_t Audio_SendNow(Audio_Chip chip,
//                             const uint8_t *data,
//                             uint16_t length)
//{
//    Audio_Context *ctx;
//    HAL_StatusTypeDef status;

//    if ((Audio_IsValidChip(chip) == 0U) || (data == 0) ||
//            (length == 0U) || (length > AUDIO_TX_BUF_LEN))
//    {
//        return 0U;
//    }

//    ctx = &s_audio_ctx[chip];
//    status = HAL_UART_Transmit(Audio_GetHandle(chip),
//                               (uint8_t *)data,
//                               length,
//                               AUDIO_UART_TIMEOUT_MS);

//    if (status != HAL_OK)
//    {
//        return 0U;
//    }

//    ctx->state = AUDIO_STATE_WAIT;
//    ctx->state_tick = HAL_GetTick();
//    return 1U;
//}

//static void Audio_RequestSend(Audio_Chip chip,
//                              const uint8_t *data,
//                              uint16_t length)
//{
//    Audio_Context *ctx;

//    if ((s_audio_initialized == 0U) ||
//            (Audio_IsValidChip(chip) == 0U) || (data == 0) ||
//            (length == 0U) || (length > AUDIO_TX_BUF_LEN))
//    {
//        return;
//    }

//    /* E15期间两块板只接受专用报警流程，普通按键、投币和背景命令全部丢弃。 */
//    if (s_exclusive_alarm_active != 0U)
//    {
//        return;
//    }

//    if ((chip == AUDIO_CHIP_2) && (g_background_music_enable == 0U))
//    {
//        return;
//    }

//    ctx = &s_audio_ctx[chip];

//    if ((ctx->state == AUDIO_STATE_IDLE) &&
//            (ctx->startup_volume_frames == 0U) &&
//            (ctx->queue_count == 0U))
//    {
//        if (Audio_SendNow(chip, data, length) != 0U)
//        {
//            return;
//        }
//    }

//    Audio_QueuePush(ctx, data, length);
//}

//void Audio_Init(void)
//{
//    uint8_t chip;

//    s_audio_initialized = 0U;
//    s_exclusive_alarm_active = 0U;
//    memset(s_audio_ctx, 0, sizeof(s_audio_ctx));
//    Audio_EffectReset();

//    for (chip = 0U; chip < (uint8_t)AUDIO_CHIP_COUNT; chip++)
//    {
//        s_audio_ctx[chip].state = AUDIO_STATE_STARTUP_WAIT;
//        s_audio_ctx[chip].target_volume = AUDIO_INIT_VOLUME;
//        s_audio_ctx[chip].startup_volume_frames =
//            AUDIO_STARTUP_VOLUME_REPEAT;
//        s_audio_ctx[chip].state_tick = HAL_GetTick();
//    }

//    s_audio_initialized = 1U;
//}

//static void Audio_TaskOne(Audio_Chip chip)
//{
//    Audio_Context *ctx;
//    uint8_t data[AUDIO_TX_BUF_LEN];
//    uint16_t length;

//    if (Audio_IsValidChip(chip) == 0U)
//    {
//        return;
//    }

//    if ((chip == AUDIO_CHIP_2) &&
//            ((g_background_music_enable == 0U) ||
//             (s_exclusive_alarm_active != 0U)))
//    {
//        return;
//    }

//    ctx = &s_audio_ctx[chip];

//    if (ctx->state == AUDIO_STATE_STARTUP_WAIT)
//    {
//        if (Audio_IsElapsed(ctx->state_tick, AUDIO_STARTUP_DELAY_MS) == 0U)
//        {
//            return;
//        }

//        ctx->state = AUDIO_STATE_IDLE;
//    }

//    if (ctx->state == AUDIO_STATE_WAIT)
//    {
//        if (Audio_IsElapsed(ctx->state_tick, AUDIO_COMMAND_GAP_MS) == 0U)
//        {
//            return;
//        }

//        ctx->state = AUDIO_STATE_IDLE;
//    }

//    if (ctx->state != AUDIO_STATE_IDLE)
//    {
//        ctx->state = AUDIO_STATE_ERROR;
//        return;
//    }

//    /*
//     * Setting_Init()在启动等待期间把EEPROM中的P13写到target_volume。
//     * KT404C初始化完成后重复写两次，避免上电先按最大音量播放。
//     */
//    if (ctx->startup_volume_frames != 0U)
//    {
//        Audio_BuildFrame(data, 0x06U, ctx->target_volume);
//        ctx->startup_volume_frames--;
//        Audio_SendNow(chip, data, AUDIO_FRAME_LEN);
//        return;
//    }

//    if (Audio_QueuePop(ctx, data, &length) != 0U)
//    {
//        if (Audio_SendNow(chip, data, length) == 0U)
//        {
//            Audio_QueuePush(ctx, data, length);
//        }
//    }
//}

//static void Audio_EffectTimeoutTask(uint32_t now)
//{
//    uint32_t elapsed;

//    if (s_effect_ctx.transient_active != 0U)
//    {
//        elapsed = (uint32_t)(now - s_effect_ctx.transient_tick);

//        if (elapsed >= s_effect_ctx.transient_audible_ms)
//        {
//            /*
//             * 到达实际有声结尾就允许恢复013，不等待MP3尾部静音。
//             * 若此时已有更新音效，更新音效优先，013暂不插回去。
//             */
//            s_effect_ctx.transient_active = 0U;
//            s_effect_ctx.advert_active = 0U;
//            s_effect_ctx.advert_index = 0U;
//            s_effect_ctx.advert_tick = 0U;
//            s_effect_ctx.advert_min_complete_ms = 0U;
//            s_effect_ctx.advert_timeout_ms = 0U;
//        }
//    }

//    if ((s_effect_ctx.base_active != 0U) &&
//            (s_effect_ctx.base_loop == 0U) &&
//            (s_effect_ctx.transient_active == 0U) &&
//            (s_effect_ctx.protected_loop_active == 0U))
//    {
//        elapsed = (uint32_t)(now - s_effect_ctx.base_tick);

//        if (elapsed >= s_effect_ctx.base_remaining_ms)
//        {
//            Audio_EffectClearBase();
//        }
//    }
//}

//static uint8_t Audio_EffectChannelReady(void)
//{
//    Audio_Context *ctx;

//    ctx = &s_audio_ctx[AUDIO_CHIP_1];

//    if ((ctx->state != AUDIO_STATE_IDLE) ||
//            (ctx->startup_volume_frames != 0U) ||
//            (ctx->queue_count != 0U))
//    {
//        return 0U;
//    }

//    return 1U;
//}

//static void Audio_EffectStartTransient(uint16_t index, uint32_t now)
//{
//    s_effect_ctx.transient_active = 1U;
//    s_effect_ctx.transient_index = index;
//    s_effect_ctx.transient_tick = now;
//    s_effect_ctx.transient_audible_ms = Audio_EffectGetAudibleMs(index);

//    if (s_effect_ctx.protected_loop_active != 0U)
//    {
//        s_effect_ctx.protected_resume_needed = 1U;
//    }
//}

///* 空闲时立即发送；120ms硬件命令间隔内只保留最后一次请求。 */
//static void Audio_EffectTryDispatch(uint32_t now)
//{
//    Audio_EffectEvent event;
//    uint8_t frame[AUDIO_FRAME_LEN];
//    uint16_t advert_file;
//    uint8_t use_advert;

//    if ((Audio_EffectChannelReady() == 0U) ||
//            (s_exclusive_alarm_active != 0U))
//    {
//        return;
//    }

//    if (Audio_EffectGetPending(&event) != 0U)
//    {
//        if (event.loop != 0U)
//        {
//            Audio_BuildFrame(frame, 0x08U, event.index);

//            if (Audio_SendNow(AUDIO_CHIP_1, frame, sizeof(frame)) != 0U)
//            {
//                Audio_EffectClearPending();
//                s_effect_ctx.protected_loop_active = 1U;
//                s_effect_ctx.protected_loop_index = event.index;
//                s_effect_ctx.protected_resume_needed = 0U;
//                s_effect_ctx.transient_active = 0U;
//                Audio_EffectStartBase(event.index, 1U, now);
//            }

//            return;
//        }

//        use_advert = 0U;

//        if ((s_effect_ctx.protected_loop_active != 0U) &&
//                (s_effect_ctx.base_active != 0U) &&
//                (s_effect_ctx.base_loop != 0U) &&
//                (s_effect_ctx.base_index == s_effect_ctx.protected_loop_index) &&
//                (s_effect_ctx.transient_active == 0U) &&
//                (event.index <= 255U))
//        {
//            use_advert = 1U;
//        }

//        if (use_advert != 0U)
//        {
//            advert_file =
//                (uint16_t)(((uint16_t)AUDIO_EFFECT_ADVERT_FOLDER << 8U) |
//                           (event.index & 0x00FFU));
//            Audio_BuildFrame(frame, 0x25U, advert_file);
//        }
//        else
//        {
//            Audio_BuildFrame(frame, 0x03U, event.index);
//        }

//        if (Audio_SendNow(AUDIO_CHIP_1, frame, sizeof(frame)) != 0U)
//        {
//            Audio_EffectClearPending();

//            if (use_advert != 0U)
//            {
//                Audio_EffectStartAdvert(event.index, now);
//            }
//            else
//            {
//                Audio_EffectStartBase(event.index, 0U, now);
//            }

//            Audio_EffectStartTransient(event.index, now);
//        }

//        return;
//    }

//    if ((s_effect_ctx.protected_loop_active != 0U) &&
//            (s_effect_ctx.protected_resume_needed != 0U) &&
//            (s_effect_ctx.transient_active == 0U))
//    {
//        Audio_BuildFrame(frame, 0x08U,
//                         s_effect_ctx.protected_loop_index);

//        if (Audio_SendNow(AUDIO_CHIP_1, frame, sizeof(frame)) != 0U)
//        {
//            s_effect_ctx.protected_resume_needed = 0U;
//            Audio_EffectStartBase(s_effect_ctx.protected_loop_index,
//                                  1U, now);
//        }
//    }
//}

//static void Audio_EffectTask(void)
//{
//    uint32_t now;

//    if ((s_audio_initialized == 0U) ||
//            (s_exclusive_alarm_active != 0U))
//    {
//        return;
//    }

//    now = HAL_GetTick();
//    Audio_EffectTimeoutTask(now);
//    Audio_EffectTryDispatch(now);
//}

//void Audio_Task(void)
//{
//    Audio_ProcessRx();
//    Audio_TaskOne(AUDIO_CHIP_1);
//    Audio_TaskOne(AUDIO_CHIP_2);
//    Audio_EffectTask();
//}

//void Audio_PlayIndexOn(Audio_Chip chip, uint16_t index)
//{
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if ((Audio_IsValidChip(chip) == 0U) || (index == 0U))
//    {
//        return;
//    }

//    if (chip == AUDIO_CHIP_1)
//    {
//        if ((s_audio_initialized != 0U) &&
//                (s_exclusive_alarm_active == 0U))
//        {
//            Audio_EffectSetPending(index, 0U);
//            Audio_EffectTryDispatch(HAL_GetTick());
//        }

//        return;
//    }

//    Audio_BuildFrame(frame, 0x03U, index);
//    Audio_RequestSend(chip, frame, sizeof(frame));
//}

//void Audio_LoopIndexOn(Audio_Chip chip, uint16_t index)
//{
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if ((Audio_IsValidChip(chip) == 0U) || (index == 0U))
//    {
//        return;
//    }

//    if (chip == AUDIO_CHIP_1)
//    {
//        if ((s_audio_initialized != 0U) &&
//                (s_exclusive_alarm_active == 0U))
//        {
//            s_effect_ctx.protected_loop_active = 1U;
//            s_effect_ctx.protected_loop_index = index;
//            s_effect_ctx.protected_resume_needed = 0U;
//            Audio_EffectSetPending(index, 1U);
//            Audio_EffectTryDispatch(HAL_GetTick());
//        }

//        return;
//    }

//    Audio_BuildFrame(frame, 0x08U, index);
//    Audio_RequestSend(chip, frame, sizeof(frame));
//}

///*
// * 只取消音效板循环音的自动恢复，不额外发送停止命令。抽奖结果音效紧接着会
// * 立即覆盖013，因此这里发送0x16反而会多制造一次120ms命令等待。
// */
//void Audio_CancelLoopResumeOn(Audio_Chip chip)
//{
//    if ((Audio_IsValidChip(chip) == 0U) ||
//            (chip != AUDIO_CHIP_1))
//    {
//        return;
//    }

//    s_effect_ctx.protected_loop_active = 0U;
//    s_effect_ctx.protected_loop_index = 0U;
//    s_effect_ctx.protected_resume_needed = 0U;

//    if ((s_effect_ctx.pending_valid != 0U) &&
//            (s_effect_ctx.pending_event.loop != 0U))
//    {
//        Audio_EffectClearPending();
//    }
//}

///* 清掉指定板的旧命令并立即发送最高优先级0x16，不等待普通队列。 */
//void Audio_StopOn(Audio_Chip chip)
//{
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if ((s_audio_initialized == 0U) ||
//            (Audio_IsValidChip(chip) == 0U))
//    {
//        return;
//    }

//    if (chip == AUDIO_CHIP_1)
//    {
//        Audio_EffectReset();
//    }

//    Audio_QueueClear(&s_audio_ctx[chip]);
//    Audio_BuildFrame(frame, 0x16U, 0U);
//    Audio_SendNow(chip, frame, sizeof(frame));
//}

///*
// * E15专用入口：两块板的旧命令全部作废，先立即停止两块板，再让音效板
// * 播放指定报警音并把当前曲目设为单曲循环。普通音效和背景命令继续被拒绝。
// *
// * 这里不再只发0x08。根据KT404C V1.6手册，0x03是已经实机验证的指定曲目
// * 播放，0x19=0是在播放状态下开启当前曲目循环；两条命令分开发送，可以确保
// * 即使某些模块固件对0x08兼容性不好，报警音也至少会立即开始播放。
// */
//void Audio_StartExclusiveAlarm(uint16_t index)
//{
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if ((s_audio_initialized == 0U) || (index == 0U))
//    {
//        return;
//    }

//    s_exclusive_alarm_active = 1U;
//    Audio_EffectReset();
//    Audio_QueueClear(&s_audio_ctx[AUDIO_CHIP_1]);
//    Audio_QueueClear(&s_audio_ctx[AUDIO_CHIP_2]);

//    Audio_BuildFrame(frame, 0x16U, 0U);
//    Audio_SendNow(AUDIO_CHIP_1, frame, sizeof(frame));
//    Audio_SendNow(AUDIO_CHIP_2, frame, sizeof(frame));

//    Audio_BuildFrame(frame, 0x03U, index);
//    Audio_QueuePush(&s_audio_ctx[AUDIO_CHIP_1], frame, sizeof(frame));

//    Audio_BuildFrame(frame, 0x19U, 0U);
//    Audio_QueuePush(&s_audio_ctx[AUDIO_CHIP_1], frame, sizeof(frame));
//}

///* E15解除时立即停止09循环；背景音乐由Game状态机重新下发。 */
//void Audio_StopExclusiveAlarm(void)
//{
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if (s_audio_initialized == 0U)
//    {
//        return;
//    }

//    Audio_QueueClear(&s_audio_ctx[AUDIO_CHIP_1]);
//    Audio_EffectReset();
//    Audio_BuildFrame(frame, 0x16U, 0U);
//    Audio_SendNow(AUDIO_CHIP_1, frame, sizeof(frame));
//    s_exclusive_alarm_active = 0U;
//}

//void Audio_VolumeUpOn(Audio_Chip chip)
//{
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if (Audio_IsValidChip(chip) == 0U)
//    {
//        return;
//    }

//    Audio_BuildFrame(frame, 0x04U, 0U);
//    Audio_RequestSend(chip, frame, sizeof(frame));
//}

//void Audio_VolumeDownOn(Audio_Chip chip)
//{
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if (Audio_IsValidChip(chip) == 0U)
//    {
//        return;
//    }

//    Audio_BuildFrame(frame, 0x05U, 0U);
//    Audio_RequestSend(chip, frame, sizeof(frame));
//}

//void Audio_SetVolumeLevelOn(Audio_Chip chip, uint8_t level)
//{
//    Audio_Context *ctx;
//    uint8_t frame[AUDIO_FRAME_LEN];

//    if (Audio_IsValidChip(chip) == 0U)
//    {
//        return;
//    }

//    if (level > AUDIO_MODULE_MAX_VOLUME)
//    {
//        level = AUDIO_MODULE_MAX_VOLUME;
//    }

//    ctx = &s_audio_ctx[chip];
//    ctx->target_volume = level;

//    if ((s_audio_initialized == 0U) ||
//            (ctx->state == AUDIO_STATE_STARTUP_WAIT) ||
//            (ctx->startup_volume_frames != 0U) ||
//            (s_exclusive_alarm_active != 0U))
//    {
//        return;
//    }

//    Audio_BuildFrame(frame, 0x06U, level);
//    Audio_RequestSend(chip, frame, sizeof(frame));
//}

//void Audio_SetVolumeLevel(uint8_t level)
//{
//    Audio_SetVolumeLevelOn(AUDIO_CHIP_1, level);
//    Audio_SetVolumeLevelOn(AUDIO_CHIP_2, level);
//}

//void Audio_SendHexOn(Audio_Chip chip, const uint8_t *hex, uint16_t len)
//{
//    if ((Audio_IsValidChip(chip) == 0U) || (hex == 0) ||
//            (len == 0U) || (len > AUDIO_TX_BUF_LEN))
//    {
//        return;
//    }

//    Audio_RequestSend(chip, hex, len);
//}

//static int8_t Audio_HexCharToValue(char ch)
//{
//    if ((ch >= '0') && (ch <= '9'))
//    {
//        return (int8_t)(ch - '0');
//    }

//    if ((ch >= 'a') && (ch <= 'f'))
//    {
//        return (int8_t)(ch - 'a' + 10);
//    }

//    if ((ch >= 'A') && (ch <= 'F'))
//    {
//        return (int8_t)(ch - 'A' + 10);
//    }

//    return -1;
//}

//static uint8_t Audio_IsSeparator(char ch)
//{
//    return ((ch == ' ') || (ch == '\t') || (ch == '\r') ||
//            (ch == '\n') || (ch == ',') || (ch == ':') ||
//            (ch == '-') || (ch == '_')) ? 1U : 0U;
//}

//static uint8_t Audio_ParseHexString(const char *str,
//                                    uint8_t *out,
//                                    uint16_t out_max,
//                                    uint16_t *out_len)
//{
//    uint16_t count;
//    int8_t high;
//    int8_t low;

//    if ((str == 0) || (out == 0) || (out_len == 0) || (out_max == 0U))
//    {
//        return 0U;
//    }

//    count = 0U;

//    while (*str != '\0')
//    {
//        while (Audio_IsSeparator(*str) != 0U)
//        {
//            str++;
//        }

//        if (*str == '\0')
//        {
//            break;
//        }

//        if ((str[0] == '0') && ((str[1] == 'x') || (str[1] == 'X')))
//        {
//            str += 2;
//        }

//        high = Audio_HexCharToValue(*str++);
//        low = Audio_HexCharToValue(*str++);

//        if ((high < 0) || (low < 0) || (count >= out_max))
//        {
//            return 0U;
//        }

//        out[count++] = (uint8_t)(((uint8_t)high << 4U) | (uint8_t)low);
//    }

//    if (count == 0U)
//    {
//        return 0U;
//    }

//    *out_len = count;
//    return 1U;
//}

//uint8_t Audio_SendHexStringOn(Audio_Chip chip, const char *hex_string)
//{
//    uint8_t data[AUDIO_TX_BUF_LEN];
//    uint16_t length;

//    if (Audio_IsValidChip(chip) == 0U)
//    {
//        return 0U;
//    }

//    length = 0U;

//    if (Audio_ParseHexString(hex_string, data, sizeof(data), &length) == 0U)
//    {
//        return 0U;
//    }

//    Audio_RequestSend(chip, data, length);
//    return 1U;
//}

//Audio_State Audio_GetStateOn(Audio_Chip chip)
//{
//    if (Audio_IsValidChip(chip) == 0U)
//    {
//        return AUDIO_STATE_ERROR;
//    }

//    return s_audio_ctx[chip].state;
//}
