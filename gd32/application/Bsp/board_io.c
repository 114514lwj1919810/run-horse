#include "board_io.h"
#include "board_timer.h"
#include "input.h"
#include "output.h"
#include "gd32f4xx.h"

#define BOARD_KEY_DEBOUNCE_MS 30U
#define BOARD_KEY_GUARD_MS    45U
#define BOARD_KEY_QUEUE_SIZE  32U
#define BOARD_HEARTBEAT_TOGGLE_MS 500U
#define BOARD_ACTIVITY_TOGGLE_MS   70U
#define BOARD_ACTIVITY_EDGE_COUNT   3U

/* Cabinet input labels are one-based; software indexes are zero-based.
 * IN1 exchange, IN3 game/start, IN4 coin pulse, IN5 setting,
 * IN7/8/9/10 up/down/left/right and IN28 fault reset. */
#define BOARD_INPUT_EXCHANGE      0U  /* IN1  */
#define BOARD_INPUT_GAME          2U  /* IN3  */
#define BOARD_INPUT_SETTING       4U  /* IN5  */
#define BOARD_INPUT_UP            6U  /* IN7  */
#define BOARD_INPUT_DOWN          7U  /* IN8  */
#define BOARD_INPUT_LEFT          8U  /* IN9  */
#define BOARD_INPUT_RIGHT         9U  /* IN10 */
#define BOARD_INPUT_SERVICE_RESET 27U /* IN28 */

typedef struct
{
    uint8_t input_index;
    uint8_t stable_pressed;
    uint8_t last_raw_pressed;
    uint32_t changed_tick;
} BoardKeyInput;

static BoardKeyInput s_keys[BOARD_KEY_COUNT];
static BoardKey s_key_queue[BOARD_KEY_QUEUE_SIZE];
static uint8_t s_key_head;
static uint8_t s_key_tail;
static uint8_t s_key_count;
static uint8_t s_has_key_press_tick;
static uint32_t s_last_key_press_tick;
static uint8_t s_status_led_on;
static uint8_t s_activity_edges_remaining;
static uint32_t s_status_led_next_tick;
static uint8_t s_coin_input=3, s_payout_input=255, s_counter_inverted;
typedef struct { uint8_t raw, stable, armed; uint32_t tick, count; } PulseCounter;
static PulseCounter s_coin_counter,s_payout_counter;

static void PollCounter(PulseCounter *c,uint32_t bits,uint8_t input,uint32_t now)
{
    uint8_t raw;
    if(input>=28) return;
    raw=(uint8_t)((((bits>>input)&1U)==0U)^s_counter_inverted);
    if(raw!=c->raw) { c->raw=raw; c->tick=now; }
    else if(raw!=c->stable && now-c->tick>=3) {
        c->stable=raw;
        if(raw) c->armed=1;
        else if(c->armed) { c->armed=0; if(c->count<0xffffffffU) c->count++; }
    }
}
void BoardIO_ConfigureCounters(uint8_t coin,uint8_t payout,uint8_t nc)
{
    uint32_t bits=IN_bit_get();
    if(coin==s_coin_input && payout==s_payout_input && nc==s_counter_inverted) return;
    s_coin_input=coin; s_payout_input=payout; s_counter_inverted=nc;
    s_coin_counter.raw=s_coin_counter.stable=(uint8_t)((((bits>>coin)&1U)==0U)^nc);
    s_payout_counter.raw=s_payout_counter.stable=payout<28?(uint8_t)((((bits>>payout)&1U)==0U)^nc):0;
    s_coin_counter.armed=s_payout_counter.armed=0;
}
uint32_t BoardIO_TakeCoinCount(void) { uint32_t n=s_coin_counter.count; s_coin_counter.count=0; return n; }
uint32_t BoardIO_TakePayoutCount(void) { uint32_t n=s_payout_counter.count; s_payout_counter.count=0; return n; }

static const uint8_t s_key_input_map[BOARD_KEY_COUNT] =
{
    BOARD_INPUT_LEFT,
    BOARD_INPUT_RIGHT,
    BOARD_INPUT_UP,
    BOARD_INPUT_DOWN,
    BOARD_INPUT_SETTING,
    BOARD_INPUT_GAME,
    BOARD_INPUT_EXCHANGE,
    BOARD_INPUT_SERVICE_RESET
};

static uint8_t BoardIO_InputPressed(uint32_t input_bits, uint8_t index)
{
    if(index >= 28U)
    {
        return 0U;
    }

    /* Inputs have external pull-ups and are active low through the key diode. */
    return ((input_bits & BIT(index)) == 0U) ? 1U : 0U;
}

static void BoardIO_PushKey(BoardKey key)
{
    if(s_key_count >= BOARD_KEY_QUEUE_SIZE)
    {
        return;
    }

    s_key_queue[s_key_tail] = key;
    s_key_tail = (uint8_t)((s_key_tail + 1U) % BOARD_KEY_QUEUE_SIZE);
    s_key_count++;
}

void BoardIO_Init(void)
{
    uint32_t input_bits;
    uint32_t now;
    BoardKey key;

    input_bits = IN_bit_get();
    now = BoardTimer_Millis();
    s_coin_counter.raw=s_coin_counter.stable=BoardIO_InputPressed(input_bits,s_coin_input);
    s_coin_counter.armed=s_coin_counter.count=0;
    s_payout_counter.armed=s_payout_counter.count=0;
    for(key = BOARD_KEY_LEFT; key < BOARD_KEY_COUNT; key = (BoardKey)(key + 1U))
    {
        s_keys[key].input_index = s_key_input_map[key];
        s_keys[key].stable_pressed = BoardIO_InputPressed(input_bits, s_keys[key].input_index);
        s_keys[key].last_raw_pressed = s_keys[key].stable_pressed;
        s_keys[key].changed_tick = now;
    }

    s_key_head = 0U;
    s_key_tail = 0U;
    s_key_count = 0U;
    s_has_key_press_tick = 0U;
    s_last_key_press_tick = now;
    s_status_led_on = 0U;
    s_activity_edges_remaining = 0U;
    s_status_led_next_tick = now + BOARD_HEARTBEAT_TOGGLE_MS;
    LED1_off();
}

void BoardIO_Poll(void)
{
    uint32_t input_bits;
    uint32_t now;
    uint8_t raw_pressed;
    uint8_t has_new_press;
    BoardKey key;
    BoardKey new_press;

    input_bits = IN_bit_get();
    now = BoardTimer_Millis();
    has_new_press = 0U;
    new_press = BOARD_KEY_COUNT;
    PollCounter(&s_coin_counter,input_bits,s_coin_input,now);
    PollCounter(&s_payout_counter,input_bits,s_payout_input,now);

    for(key = BOARD_KEY_LEFT; key < BOARD_KEY_COUNT; key = (BoardKey)(key + 1U))
    {
        if(s_keys[key].input_index==s_coin_input || s_keys[key].input_index==s_payout_input) continue;
        raw_pressed = BoardIO_InputPressed(input_bits, s_keys[key].input_index);
        if(raw_pressed != s_keys[key].last_raw_pressed)
        {
            s_keys[key].last_raw_pressed = raw_pressed;
            s_keys[key].changed_tick = now;
        }
        else if(((now - s_keys[key].changed_tick) >= BOARD_KEY_DEBOUNCE_MS) &&
                (raw_pressed != s_keys[key].stable_pressed))
        {
            s_keys[key].stable_pressed = raw_pressed;
            if((raw_pressed != 0U) && (has_new_press == 0U))
            {
                new_press = key;
                has_new_press = 1U;
            }
        }

    }

    /* Coalesce near-simultaneous ghost edges without waiting for every input
       line to release. Each debounced key transition is still emitted once. */
    if((has_new_press != 0U) &&
       ((s_has_key_press_tick == 0U) ||
        ((now - s_last_key_press_tick) >= BOARD_KEY_GUARD_MS)))
    {
        BoardIO_PushKey(new_press);
        s_last_key_press_tick = now;
        s_has_key_press_tick = 1U;
    }

    if((int32_t)(now - s_status_led_next_tick) < 0)
    {
        return;
    }

    if(s_activity_edges_remaining != 0U)
    {
        s_status_led_on ^= 1U;
        if(s_status_led_on != 0U)
        {
            LED1_on();
        }
        else
        {
            LED1_off();
        }

        s_activity_edges_remaining--;
        s_status_led_next_tick += BOARD_ACTIVITY_TOGGLE_MS;
        if(s_activity_edges_remaining == 0U)
        {
            s_status_led_on = 0U;
            LED1_off();
            s_status_led_next_tick = now + BOARD_HEARTBEAT_TOGGLE_MS;
        }
        return;
    }

    s_status_led_on ^= 1U;
    if(s_status_led_on != 0U)
    {
        LED1_on();
    }
    else
    {
        LED1_off();
    }
    s_status_led_next_tick += BOARD_HEARTBEAT_TOGGLE_MS;
}

uint8_t BoardIO_ReadInput(uint8_t index)
{
    return BoardIO_InputPressed(IN_bit_get(), index);
}

void BoardIO_WriteOutput(uint8_t index, uint8_t active)
{
    uint32_t mask;

    if(index >= 16U)
    {
        return;
    }

    mask = BIT(index);
    if(active != 0U)
    {
        OUT_bit_set(mask);
    }
    else
    {
        OUT_bit_reset(mask);
    }
}

uint8_t BoardIO_GetKeyPress(BoardKey *key)
{
    if((key == 0) || (s_key_count == 0U))
    {
        return 0U;
    }

    *key = s_key_queue[s_key_head];
    s_key_head = (uint8_t)((s_key_head + 1U) % BOARD_KEY_QUEUE_SIZE);
    s_key_count--;
    return 1U;
}

void BoardIO_StartAndroidLedBlink(void)
{
    BoardIO_NotifyCommunicationActivity();
}

void BoardIO_NotifyCommunicationActivity(void)
{
    s_status_led_on = 1U;
    LED1_on();
    s_activity_edges_remaining = BOARD_ACTIVITY_EDGE_COUNT;
    s_status_led_next_tick = BoardTimer_Millis() + BOARD_ACTIVITY_TOGGLE_MS;
}
