#pragma once
#include "../stdafx.h"
#ifdef WIN32
#include <bcrypt.h>

#define DES_ENCRYPT 0
#define DES_DECRYPT 1
#else
#include <openssl\des.h>
#endif
#include "../util/util.h"
#include "../util/utilexception.h"

#define DESKEY_LENGHT 8

class CDES3
{
	DWORD Des3(const ByteArray &data, ByteDynArray &resp, int encOp);
#ifdef WIN32
	BCRYPT_KEY_HANDLE key;
	ByteDynArray iv;
#else
	des_key_schedule k1, k2, k3;
	des_cblock initVec;
#endif

public:
	CDES3();
	CDES3(const ByteArray &key);
	~CDES3(void);

	void Init(const ByteArray &key);
	ByteDynArray Encode(const ByteArray &data);
	ByteDynArray Decode(const ByteArray &data);
	ByteDynArray RawEncode(const ByteArray &data);
	ByteDynArray RawDecode(const ByteArray &data);
};
