#ifndef _GAME_AUTHORITY_H
#define _GAME_AUTHORITY_H

#include <stdint.h>

#define GAME_AUTHORITY_ANIMAL_COUNT        6U
#define GAME_AUTHORITY_DEFAULT_COIN        10U
#define GAME_AUTHORITY_DEFAULT_BET         10U
#define GAME_AUTHORITY_DEFAULT_MAX_BET     1000U
#define GAME_AUTHORITY_DEFAULT_PAYOUT_UNIT 100U
#define GAME_AUTHORITY_SETTINGS_COUNT      37U
#define GAME_AUTHORITY_COLLECT_INTERVAL_MS     1000U
#define GAME_AUTHORITY_COLLECT_POINTS_PER_TICK 10U
#define GAME_AUTHORITY_COLLECT_POINTS_PER_SECOND 10U

typedef enum
{
    GAME_STATE_IDLE = 0,
    GAME_STATE_BETTING,
    GAME_STATE_RACE_PREPARE,
    GAME_STATE_WAIT_RACE_OVER,
    GAME_STATE_RESULT,
    GAME_STATE_WAIT_RESULT_OVER,
    GAME_STATE_WIN_WAIT_START,
    GAME_STATE_WIN_COLLECTING,
    GAME_STATE_SETTING,
    GAME_STATE_PAYOUT,
    GAME_STATE_FAULT
} GameState;

typedef struct
{
    uint32_t race_duration_ms;
    uint32_t result_duration_ms;
    uint32_t coin_credit_points;
    uint32_t bet_points;
    uint32_t max_round_bet;
    uint32_t payout_unit_points;
    uint32_t target_rtp_bp;
    uint32_t base_odds[GAME_AUTHORITY_ANIMAL_COUNT];
    uint32_t volume_percent;
    uint32_t device_id;
    uint32_t serial_debug_enabled;
    uint32_t control_level;
    uint32_t small_pace_percent;
    uint32_t multiplier_mode;
    uint32_t lion_max_bet;
    uint32_t other_max_bet;
    uint32_t ticket_coins;
    uint32_t machine_mode;
    uint32_t branch_number;
    uint32_t branch_volume;
    uint32_t branch_type;
    uint32_t branch_source;
    uint32_t acceptor_type;
    uint32_t acceptor_source;
    uint32_t running_minutes;
    uint32_t system_formula;
    uint32_t system_password;
    uint32_t max_round_award;
    uint32_t return_band_bp;
    uint32_t coin_input;
    uint32_t payout_input;
    uint32_t payout_output;
} GameSettings;

typedef struct
{
    uint64_t coin_in_count, coin_in_value;
    uint64_t commitment, payout_count, payout_value;
    uint64_t bet_total, rounds, jackpot_count, ticket_count;
    uint64_t runtime_seconds;
    uint64_t report_coin_in, report_coin_out, report_in_value, report_out_value;
    uint32_t report_count;
} AccountData;

typedef struct
{
    /* Recovery budget in point-basis-points. Kept in the old field slot for EEPROM compatibility. */
    uint64_t pace_scaled;
    uint32_t rng, tie_seed, jackpot_debt;
    uint8_t remaining[5];
    uint8_t token, token_valid, odds_mode;
    uint8_t miss_streak, guarantee_threshold, accounting_version, recovery_active;
} WinnerControlState;

typedef struct
{
    uint32_t award;
    int64_t headroom;
    uint8_t winner, tier, paced, valid;
} WinnerPlan;

typedef struct
{
    GameState state;
    GameSettings settings;
    uint8_t setting_index;
    uint32_t round_id;
    uint32_t balance_points;
    uint32_t bets[GAME_AUTHORITY_ANIMAL_COUNT];
    uint32_t odds[GAME_AUTHORITY_ANIMAL_COUNT];
    uint32_t locked_bets[GAME_AUTHORITY_ANIMAL_COUNT];
    uint8_t selected_id;
    uint8_t winner_id;
    uint32_t win_points;
    uint32_t win_total_points;
    uint32_t win_remaining_points;
    uint32_t last_win_points;
    uint32_t pending_payout_coins;
    uint32_t fault_code;
    uint32_t seq;
    uint32_t race_seq;
    uint32_t result_seq;
    uint32_t race_deadline_tick;
    uint32_t result_deadline_tick;
    uint32_t collect_seq;
    uint32_t collect_next_tick;
    uint32_t collect_deadline_tick;
    uint32_t collect_last_delta;
    uint8_t collect_fast_done;
    uint8_t race_result_credited;
    uint32_t locked_odds[GAME_AUTHORITY_ANIMAL_COUNT];
    uint32_t committed_award;
    uint32_t settings_revision;
    uint32_t payout_target, payout_done, payout_unit_locked;
    uint32_t last_settings_request, last_settings_base;
    uint32_t last_report_request;
    AccountData account;
    WinnerControlState control;
    WinnerPlan plan;
    uint32_t plan_dirty;
} GameAuthorityState;

#define PLAN_DIRTY_BET        1U
#define PLAN_DIRTY_ODDS       2U
#define PLAN_DIRTY_COIN_IN    4U
#define PLAN_DIRTY_PAYOUT     8U
#define PLAN_DIRTY_COMMITMENT 16U
#define PLAN_DIRTY_POOL       32U
#define PLAN_DIRTY_SETTINGS   64U

void GameSettings_Default(GameSettings *settings);
uint8_t GameSettings_Validate(const GameSettings *settings);
uint32_t GameSettings_Get(const GameSettings *settings, uint8_t index);
uint8_t GameSettings_Set(GameSettings *settings, uint8_t index, uint32_t value);
void WinnerControl_Begin(GameAuthorityState *state);
void WinnerControl_Evaluate(GameAuthorityState *state);
void WinnerControl_Commit(GameAuthorityState *state);
uint8_t WinnerControl_Migrate(GameAuthorityState *state);
uint8_t GameAuthority_ConfirmPayout(GameAuthorityState *state, uint32_t count);
void GameAuthority_RestoreTimers(GameAuthorityState *state, uint32_t now);

void GameAuthority_Init(GameAuthorityState *state);
uint32_t GameAuthority_TotalBet(const GameAuthorityState *state);
void GameAuthority_UpdateOdds(GameAuthorityState *state);
uint8_t GameAuthority_AddCoin(GameAuthorityState *state);
uint8_t GameAuthority_SelectPrevious(GameAuthorityState *state);
uint8_t GameAuthority_SelectNext(GameAuthorityState *state);
uint8_t GameAuthority_PlaceBet(GameAuthorityState *state);
uint8_t GameAuthority_CanPrepareRace(const GameAuthorityState *state);
uint8_t GameAuthority_PrepareRace(GameAuthorityState *state, uint32_t now_tick);
uint8_t GameAuthority_HandleRaceOver(GameAuthorityState *state, uint32_t seq, uint32_t round, uint32_t now_tick);
uint8_t GameAuthority_HandleRaceTimeout(GameAuthorityState *state, uint32_t now_tick);
uint8_t GameAuthority_HandleResultOver(GameAuthorityState *state, uint32_t seq, uint32_t round);
uint8_t GameAuthority_HandleResultTimeout(GameAuthorityState *state, uint32_t now_tick);
uint8_t GameAuthority_StartWinCollect(GameAuthorityState *state, uint32_t now_tick);
uint8_t GameAuthority_PollWinCollect(GameAuthorityState *state, uint32_t now_tick);
uint8_t GameAuthority_FastFinishWinCollect(GameAuthorityState *state);
uint8_t GameAuthority_ToggleSetting(GameAuthorityState *state);
uint8_t GameAuthority_SettingPrevious(GameAuthorityState *state);
uint8_t GameAuthority_SettingNext(GameAuthorityState *state);
uint8_t GameAuthority_SettingAdjustDown(GameAuthorityState *state);
uint8_t GameAuthority_SettingAdjustUp(GameAuthorityState *state);
uint8_t GameAuthority_StartPayout(GameAuthorityState *state);
uint8_t GameAuthority_FinishPayout(GameAuthorityState *state);
uint8_t GameAuthority_ServiceReset(GameAuthorityState *state);
uint32_t GameAuthority_NextSequence(GameAuthorityState *state);
const char *GameAuthority_StateName(GameState state);
uint32_t GameAuthority_SettingValue(const GameAuthorityState *state, uint8_t index);
const char *GameAuthority_SettingItemName(uint8_t index);
uint32_t GameAuthority_SettingMinimum(uint8_t index);
uint32_t GameAuthority_SettingMaximum(uint8_t index);

#endif
