#ifndef TEST_EEPROM_H
#define TEST_EEPROM_H
#include <stdint.h>
int AT24CS64_ReadBuffer(uint16_t address,uint8_t *data,uint16_t length);
int AT24CS64_WriteBuffer(uint16_t address,const uint8_t *data,uint16_t length);
#endif
