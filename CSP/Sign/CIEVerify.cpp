#include "CIEVerify.h"
#include "../Util/log.h"

CIEVerify::CIEVerify()
{

}

long CIEVerify::verify(const char* input_file, VERIFY_RESULT* verifyResult)
{
	
		try {

			DISIGON_CTX ctx;

			long ret;

			ctx = disigon_verify_init();

			ret = disigon_set(DISIGON_OPT_LOG_FILE, (void*)"F:\\Projects\\IPZS\\TestFirmaCIE\\log.txt");

			ret = disigon_set(DISIGON_OPT_LOG_LEVEL, (void*)LOG_TYPE_DEBUG);

			ret = disigon_verify_set(ctx, DISIGON_OPT_INPUTFILE, (void*)input_file);
			if (ret != 0)
			{
				throw ret;
			}

			ret = disigon_verify_set(ctx, DISIGON_OPT_INPUTFILE_TYPE, (void*)DISIGON_FILETYPE_AUTO);
			if (ret != 0)
			{
				throw ret;
			}

			//ret = disigon_verify_set(ctx, DISIGON_OPT_INPUTFILE_PLAINTEXT, "input-restored.txt");

			//PARAMETRO 0 non usa verifica OCSP
			//PARAMETRO 1 OK OCSP
			ret = disigon_verify_set(ctx, DISIGON_OPT_VERIFY_REVOCATION, (void*)1);
			if (ret != 0)
			{
				throw ret;
			}

			ret = disigon_verify_verify(ctx, verifyResult);
			if (ret != 0)
			{
				throw ret;
			}

			ret = disigon_verify_cleanup(ctx);
			if (ret != 0)
			{
				throw ret;
			}

			return ret;

	}
	catch (long err) {
		Log.write("CIEVerify::verify error: %lx", err);
	}
}
