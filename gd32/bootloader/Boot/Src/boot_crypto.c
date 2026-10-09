#include "boot_crypto.h"

#include <string.h>

static const uint32_t s_sha_k[64] =
{
    0x428A2F98UL,0x71374491UL,0xB5C0FBCFUL,0xE9B5DBA5UL,
    0x3956C25BUL,0x59F111F1UL,0x923F82A4UL,0xAB1C5ED5UL,
    0xD807AA98UL,0x12835B01UL,0x243185BEUL,0x550C7DC3UL,
    0x72BE5D74UL,0x80DEB1FEUL,0x9BDC06A7UL,0xC19BF174UL,
    0xE49B69C1UL,0xEFBE4786UL,0x0FC19DC6UL,0x240CA1CCUL,
    0x2DE92C6FUL,0x4A7484AAUL,0x5CB0A9DCUL,0x76F988DAUL,
    0x983E5152UL,0xA831C66DUL,0xB00327C8UL,0xBF597FC7UL,
    0xC6E00BF3UL,0xD5A79147UL,0x06CA6351UL,0x14292967UL,
    0x27B70A85UL,0x2E1B2138UL,0x4D2C6DFCUL,0x53380D13UL,
    0x650A7354UL,0x766A0ABBUL,0x81C2C92EUL,0x92722C85UL,
    0xA2BFE8A1UL,0xA81A664BUL,0xC24B8B70UL,0xC76C51A3UL,
    0xD192E819UL,0xD6990624UL,0xF40E3585UL,0x106AA070UL,
    0x19A4C116UL,0x1E376C08UL,0x2748774CUL,0x34B0BCB5UL,
    0x391C0CB3UL,0x4ED8AA4AUL,0x5B9CCA4FUL,0x682E6FF3UL,
    0x748F82EEUL,0x78A5636FUL,0x84C87814UL,0x8CC70208UL,
    0x90BEFFFAUL,0xA4506CEBUL,0xBEF9A3F7UL,0xC67178F2UL
};

static const uint8_t s_aes_sbox[256] =
{
    0x63,0x7C,0x77,0x7B,0xF2,0x6B,0x6F,0xC5,0x30,0x01,0x67,0x2B,0xFE,0xD7,0xAB,0x76,
    0xCA,0x82,0xC9,0x7D,0xFA,0x59,0x47,0xF0,0xAD,0xD4,0xA2,0xAF,0x9C,0xA4,0x72,0xC0,
    0xB7,0xFD,0x93,0x26,0x36,0x3F,0xF7,0xCC,0x34,0xA5,0xE5,0xF1,0x71,0xD8,0x31,0x15,
    0x04,0xC7,0x23,0xC3,0x18,0x96,0x05,0x9A,0x07,0x12,0x80,0xE2,0xEB,0x27,0xB2,0x75,
    0x09,0x83,0x2C,0x1A,0x1B,0x6E,0x5A,0xA0,0x52,0x3B,0xD6,0xB3,0x29,0xE3,0x2F,0x84,
    0x53,0xD1,0x00,0xED,0x20,0xFC,0xB1,0x5B,0x6A,0xCB,0xBE,0x39,0x4A,0x4C,0x58,0xCF,
    0xD0,0xEF,0xAA,0xFB,0x43,0x4D,0x33,0x85,0x45,0xF9,0x02,0x7F,0x50,0x3C,0x9F,0xA8,
    0x51,0xA3,0x40,0x8F,0x92,0x9D,0x38,0xF5,0xBC,0xB6,0xDA,0x21,0x10,0xFF,0xF3,0xD2,
    0xCD,0x0C,0x13,0xEC,0x5F,0x97,0x44,0x17,0xC4,0xA7,0x7E,0x3D,0x64,0x5D,0x19,0x73,
    0x60,0x81,0x4F,0xDC,0x22,0x2A,0x90,0x88,0x46,0xEE,0xB8,0x14,0xDE,0x5E,0x0B,0xDB,
    0xE0,0x32,0x3A,0x0A,0x49,0x06,0x24,0x5C,0xC2,0xD3,0xAC,0x62,0x91,0x95,0xE4,0x79,
    0xE7,0xC8,0x37,0x6D,0x8D,0xD5,0x4E,0xA9,0x6C,0x56,0xF4,0xEA,0x65,0x7A,0xAE,0x08,
    0xBA,0x78,0x25,0x2E,0x1C,0xA6,0xB4,0xC6,0xE8,0xDD,0x74,0x1F,0x4B,0xBD,0x8B,0x8A,
    0x70,0x3E,0xB5,0x66,0x48,0x03,0xF6,0x0E,0x61,0x35,0x57,0xB9,0x86,0xC1,0x1D,0x9E,
    0xE1,0xF8,0x98,0x11,0x69,0xD9,0x8E,0x94,0x9B,0x1E,0x87,0xE9,0xCE,0x55,0x28,0xDF,
    0x8C,0xA1,0x89,0x0D,0xBF,0xE6,0x42,0x68,0x41,0x99,0x2D,0x0F,0xB0,0x54,0xBB,0x16
};

static uint32_t rotr32(uint32_t value, uint32_t bits)
{
    return (value >> bits) | (value << (32U - bits));
}

static uint32_t load_be32(const uint8_t *p)
{
    return ((uint32_t)p[0] << 24U) | ((uint32_t)p[1] << 16U) |
           ((uint32_t)p[2] << 8U) | p[3];
}

static void store_be32(uint8_t *p, uint32_t value)
{
    p[0] = (uint8_t)(value >> 24U); p[1] = (uint8_t)(value >> 16U);
    p[2] = (uint8_t)(value >> 8U);  p[3] = (uint8_t)value;
}

static void store_be64(uint8_t *p, uint64_t value)
{
    uint32_t i;
    for (i = 0U; i < 8U; ++i) p[7U-i] = (uint8_t)(value >> (i*8U));
}

static void sha_transform(BootSha256 *context, const uint8_t block[64])
{
    uint32_t w[64]; uint32_t a,b,c,d,e,f,g,h,t1,t2,i;
    for (i=0U;i<16U;++i) w[i]=load_be32(block+i*4U);
    for (;i<64U;++i)
    {
        uint32_t s0=rotr32(w[i-15U],7U)^rotr32(w[i-15U],18U)^(w[i-15U]>>3U);
        uint32_t s1=rotr32(w[i-2U],17U)^rotr32(w[i-2U],19U)^(w[i-2U]>>10U);
        w[i]=w[i-16U]+s0+w[i-7U]+s1;
    }
    a=context->state[0];b=context->state[1];c=context->state[2];d=context->state[3];
    e=context->state[4];f=context->state[5];g=context->state[6];h=context->state[7];
    for(i=0U;i<64U;++i)
    {
        uint32_t s1=rotr32(e,6U)^rotr32(e,11U)^rotr32(e,25U);
        uint32_t ch=(e&f)^((~e)&g);
        uint32_t s0=rotr32(a,2U)^rotr32(a,13U)^rotr32(a,22U);
        uint32_t maj=(a&b)^(a&c)^(b&c);
        t1=h+s1+ch+s_sha_k[i]+w[i];t2=s0+maj;
        h=g;g=f;f=e;e=d+t1;d=c;c=b;b=a;a=t1+t2;
    }
    context->state[0]+=a;context->state[1]+=b;context->state[2]+=c;context->state[3]+=d;
    context->state[4]+=e;context->state[5]+=f;context->state[6]+=g;context->state[7]+=h;
}

void BootCrypto_Sha256Init(BootSha256 *context)
{
    static const uint32_t initial[8]={0x6A09E667UL,0xBB67AE85UL,0x3C6EF372UL,0xA54FF53AUL,
        0x510E527FUL,0x9B05688CUL,0x1F83D9ABUL,0x5BE0CD19UL};
    memcpy(context->state,initial,sizeof(initial));context->total_size=0U;context->block_size=0U;
}

void BootCrypto_Sha256Update(BootSha256 *context,const uint8_t *data,uint32_t size)
{
    uint32_t n;
    context->total_size+=size;
    while(size)
    {
        n=64U-context->block_size;if(n>size)n=size;
        memcpy(context->block+context->block_size,data,n);
        context->block_size+=n;data+=n;size-=n;
        if(context->block_size==64U){sha_transform(context,context->block);context->block_size=0U;}
    }
}

void BootCrypto_Sha256Final(BootSha256 *context,uint8_t output[32])
{
    uint64_t bits=context->total_size*8U;uint32_t i;
    context->block[context->block_size++]=0x80U;
    if(context->block_size>56U){while(context->block_size<64U)context->block[context->block_size++]=0U;sha_transform(context,context->block);context->block_size=0U;}
    while(context->block_size<56U)context->block[context->block_size++]=0U;
    store_be64(context->block+56U,bits);sha_transform(context,context->block);
    for(i=0U;i<8U;++i)store_be32(output+i*4U,context->state[i]);
    memset(context,0,sizeof(*context));
}

void BootCrypto_HmacSha256(const uint8_t *key,uint32_t key_size,const uint8_t *data,uint32_t data_size,uint8_t output[32])
{
    uint8_t k[64],inner[32];uint32_t i;BootSha256 context;
    memset(k,0,sizeof(k));
    if(key_size>64U){BootCrypto_Sha256Init(&context);BootCrypto_Sha256Update(&context,key,key_size);BootCrypto_Sha256Final(&context,k);}else memcpy(k,key,key_size);
    for(i=0U;i<64U;++i)k[i]^=0x36U;
    BootCrypto_Sha256Init(&context);BootCrypto_Sha256Update(&context,k,64U);BootCrypto_Sha256Update(&context,data,data_size);BootCrypto_Sha256Final(&context,inner);
    for(i=0U;i<64U;++i)k[i]^=(0x36U^0x5CU);
    BootCrypto_Sha256Init(&context);BootCrypto_Sha256Update(&context,k,64U);BootCrypto_Sha256Update(&context,inner,32U);BootCrypto_Sha256Final(&context,output);
    memset(k,0,sizeof(k));memset(inner,0,sizeof(inner));
}

void BootCrypto_DeriveKey(const uint8_t device_key[32],const uint8_t uid[12],const uint8_t label[8],uint32_t model,const char purpose[3],uint8_t output[32])
{
    static const uint8_t salt[]="ANIMAL-RACE-SECURE-IAP-v1";
    uint8_t prk[32],info[28];
    BootCrypto_HmacSha256(salt,(uint32_t)(sizeof(salt)-1U),device_key,32U,prk);
    memcpy(info,purpose,3U);memcpy(info+3U,uid,12U);memcpy(info+15U,label,8U);
    info[23]=(uint8_t)model;info[24]=(uint8_t)(model>>8U);info[25]=(uint8_t)(model>>16U);info[26]=(uint8_t)(model>>24U);info[27]=1U;
    BootCrypto_HmacSha256(prk,32U,info,sizeof(info),output);
    memset(prk,0,sizeof(prk));memset(info,0,sizeof(info));
}

static uint8_t aes_xtime(uint8_t x){return (uint8_t)((x<<1U)^((x&0x80U)?0x1BU:0U));}

static void aes_expand(const uint8_t key[32],uint8_t round_key[240])
{
    uint32_t generated=32U,rcon=1U,i;uint8_t t[4],v;
    memcpy(round_key,key,32U);
    while(generated<240U)
    {
        memcpy(t,round_key+generated-4U,4U);
        if((generated&31U)==0U)
        {
            v=t[0];t[0]=s_aes_sbox[t[1]];t[1]=s_aes_sbox[t[2]];t[2]=s_aes_sbox[t[3]];t[3]=s_aes_sbox[v];
            t[0]^=(uint8_t)rcon;rcon=aes_xtime((uint8_t)rcon);
        }
        else if((generated&31U)==16U)for(i=0U;i<4U;++i)t[i]=s_aes_sbox[t[i]];
        for(i=0U;i<4U;++i){round_key[generated]=round_key[generated-32U]^t[i];++generated;}
    }
}

static void aes_encrypt(const uint8_t round_key[240],const uint8_t input[16],uint8_t output[16])
{
    uint8_t s[16],t,a0,a1,a2,a3;uint32_t round,i,c;
    for(i=0U;i<16U;++i)s[i]=input[i]^round_key[i];
    for(round=1U;round<=14U;++round)
    {
        for(i=0U;i<16U;++i)s[i]=s_aes_sbox[s[i]];
        t=s[1];s[1]=s[5];s[5]=s[9];s[9]=s[13];s[13]=t;
        t=s[2];s[2]=s[10];s[10]=t;t=s[6];s[6]=s[14];s[14]=t;
        t=s[15];s[15]=s[11];s[11]=s[7];s[7]=s[3];s[3]=t;
        if(round<14U)for(c=0U;c<4U;++c)
        {
            i=c*4U;a0=s[i];a1=s[i+1U];a2=s[i+2U];a3=s[i+3U];
            s[i]=aes_xtime(a0)^(aes_xtime(a1)^a1)^a2^a3;
            s[i+1U]=a0^aes_xtime(a1)^(aes_xtime(a2)^a2)^a3;
            s[i+2U]=a0^a1^aes_xtime(a2)^(aes_xtime(a3)^a3);
            s[i+3U]=(aes_xtime(a0)^a0)^a1^a2^aes_xtime(a3);
        }
        for(i=0U;i<16U;++i)s[i]^=round_key[round*16U+i];
    }
    memcpy(output,s,16U);memset(s,0,sizeof(s));
}

static void ghash_multiply(uint8_t x[16],const uint8_t h[16])
{
    uint8_t z[16]={0},v[16];uint32_t i,j;uint8_t lsb;
    memcpy(v,h,16U);
    for(i=0U;i<128U;++i)
    {
        if(x[i>>3U]&(uint8_t)(0x80U>>(i&7U)))for(j=0U;j<16U;++j)z[j]^=v[j];
        lsb=v[15]&1U;
        for(j=15U;j>0U;--j)v[j]=(uint8_t)((v[j]>>1U)|(v[j-1U]<<7U));
        v[0]>>=1U;if(lsb)v[0]^=0xE1U;
    }
    memcpy(x,z,16U);memset(z,0,sizeof(z));memset(v,0,sizeof(v));
}

static void ghash_block(BootGcmAuth *context,const uint8_t block[16])
{
    uint32_t i;for(i=0U;i<16U;++i)context->ghash[i]^=block[i];ghash_multiply(context->ghash,context->hash_subkey);
}

static void ghash_update(BootGcmAuth *context,const uint8_t *data,uint32_t size)
{
    uint32_t n;
    while(size){n=16U-context->partial_size;if(n>size)n=size;memcpy(context->partial+context->partial_size,data,n);context->partial_size+=n;data+=n;size-=n;if(context->partial_size==16U){ghash_block(context,context->partial);context->partial_size=0U;}}
}

static void ghash_pad(BootGcmAuth *context)
{
    if(context->partial_size){memset(context->partial+context->partial_size,0,16U-context->partial_size);ghash_block(context,context->partial);context->partial_size=0U;}
}

void BootCrypto_GcmAuthInit(BootGcmAuth *context,const uint8_t key[32],const uint8_t nonce[12],const uint8_t *aad,uint32_t aad_size)
{
    uint8_t zero[16]={0};memset(context,0,sizeof(*context));aes_expand(key,context->round_key);aes_encrypt(context->round_key,zero,context->hash_subkey);
    memcpy(context->j0,nonce,12U);context->j0[15]=1U;context->aad_size=aad_size;
    ghash_update(context,aad,aad_size);ghash_pad(context);
}

void BootCrypto_GcmAuthUpdate(BootGcmAuth *context,const uint8_t *ciphertext,uint32_t size)
{
    context->cipher_size+=size;ghash_update(context,ciphertext,size);
}

void BootCrypto_GcmAuthFinish(BootGcmAuth *context,uint8_t tag[16])
{
    uint8_t length_block[16],mask[16];uint32_t i;ghash_pad(context);
    store_be64(length_block,context->aad_size*8U);store_be64(length_block+8U,context->cipher_size*8U);ghash_block(context,length_block);
    aes_encrypt(context->round_key,context->j0,mask);for(i=0U;i<16U;++i)tag[i]=mask[i]^context->ghash[i];memset(mask,0,sizeof(mask));
}

static void increment_counter(uint8_t counter[16])
{
    uint32_t i;for(i=16U;i>12U;--i){++counter[i-1U];if(counter[i-1U])break;}
}

void BootCrypto_GcmCtrInit(BootGcmCtr *context,const uint8_t key[32],const uint8_t nonce[12])
{
    memset(context,0,sizeof(*context));aes_expand(key,context->round_key);memcpy(context->counter,nonce,12U);context->counter[15]=1U;context->stream_used=16U;
}

void BootCrypto_GcmCtrCrypt(BootGcmCtr *context,const uint8_t *input,uint8_t *output,uint32_t size)
{
    uint32_t i;for(i=0U;i<size;++i){if(context->stream_used==16U){increment_counter(context->counter);aes_encrypt(context->round_key,context->counter,context->stream);context->stream_used=0U;}output[i]=input[i]^context->stream[context->stream_used++];}
}

int BootCrypto_Equal(const uint8_t *left,const uint8_t *right,uint32_t size)
{
    uint8_t difference=0U;uint32_t i;for(i=0U;i<size;++i)difference|=left[i]^right[i];return difference==0U;
}

int BootCrypto_SelfTest(void)
{
    static const uint8_t sha_abc[32]={0xBA,0x78,0x16,0xBF,0x8F,0x01,0xCF,0xEA,0x41,0x41,0x40,0xDE,0x5D,0xAE,0x22,0x23,0xB0,0x03,0x61,0xA3,0x96,0x17,0x7A,0x9C,0xB4,0x10,0xFF,0x61,0xF2,0x00,0x15,0xAD};
    static const uint8_t cipher[16]={0xCE,0xA7,0x40,0x3D,0x4D,0x60,0x6B,0x6E,0x07,0x4E,0xC5,0xD3,0xBA,0xF3,0x9D,0x18};
    static const uint8_t expected_tag[16]={0xD0,0xD1,0xC8,0xA7,0x99,0x99,0x6B,0xF0,0x26,0x5B,0x98,0xB5,0xD4,0x8A,0xB9,0x19};
    uint8_t zero_key[32]={0},zero_nonce[12]={0},plain[16],tag[16],digest[32];BootSha256 sha;BootGcmAuth auth;BootGcmCtr ctr;uint32_t i;
    BootCrypto_Sha256Init(&sha);BootCrypto_Sha256Update(&sha,(const uint8_t *)"abc",3U);BootCrypto_Sha256Final(&sha,digest);if(!BootCrypto_Equal(digest,sha_abc,32U))return 0;
    BootCrypto_GcmAuthInit(&auth,zero_key,zero_nonce,0,0U);BootCrypto_GcmAuthUpdate(&auth,cipher,16U);BootCrypto_GcmAuthFinish(&auth,tag);if(!BootCrypto_Equal(tag,expected_tag,16U))return 0;
    BootCrypto_GcmCtrInit(&ctr,zero_key,zero_nonce);BootCrypto_GcmCtrCrypt(&ctr,cipher,plain,16U);for(i=0U;i<16U;++i)if(plain[i]!=0U)return 0;
    memset(&auth,0,sizeof(auth));memset(&ctr,0,sizeof(ctr));return 1;
}
