/*
 * Copyright (c) 2016 The ZLMediaKit project authors. All Rights Reserved.
 *
 * This file is part of ZLMediaKit(https://github.com/xiongziliang/ZLMediaKit).
 *
 * Use of this source code is governed by MIT license that can be found in the
 * LICENSE file in the root of the source tree. All contributing project authors
 * may be found in the AUTHORS file in the root of the source tree.
 */

#include "AACRtpTranscode.h"
#define AAC_MAX_FRAME_SIZE (2 * 1024)

namespace mediakit{
/////////////////////////////////////////////////////////////////////////////////////
//AACRtpTranscodeDecoder::AACRtpTranscodeDecoder(const Track::Ptr &track) {
//	auto aacTrack = dynamic_pointer_cast<AACTrack>(track);
//	if (!aacTrack || !aacTrack->ready()) {
//		WarnL << "该aac track无效!";
//	}
//	else {
//		_aac_cfg = aacTrack->getAacCfg();
//	}
//	_frame = obtainFrame();
//}

AACRtpTranscodeDecoder::AACRtpTranscodeDecoder(const Track::Ptr &track) {
	auto audioTrack = dynamic_pointer_cast<AudioTrack>(track);
	_frame = obtainFrame();
	InitParam initParam_rtsp;
	initParam_rtsp.u32AudioSamplerate = audioTrack->getAudioSampleRate();
	initParam_rtsp.ucAudioChannel = audioTrack->getAudioChannel();
	initParam_rtsp.u32PCMBitSize = audioTrack->getAudioSampleBit();
	initParam_rtsp.ucAudioCodec = audioTrack->getCodecId() == CodecG711A ? Law_ALaw : Law_ULaw;
	handle_rtsp = Easy_AACEncoder_Init(initParam_rtsp);
}
AACFrame::Ptr AACRtpTranscodeDecoder::obtainFrame() {
    //从缓存池重新申请对象，防止覆盖已经写入环形缓存的对象
    auto frame = ResourcePoolHelper<AACFrame>::obtainObj();
    frame->_prefix_size = 0;
    frame->_buffer.clear();
    return frame;
}

bool AACRtpTranscodeDecoder::inputRtp(const RtpPacket::Ptr &rtppack, bool key_pos) {


	int length = rtppack->size() - rtppack->offset;
	// 获取rtp数据
	const char *rtp_packet_buf = rtppack->data() + rtppack->offset;
	string rtpHead = std::string(rtppack->data(), rtppack->offset);
	bG711ABufferSize_rtsp = length;
	int bAACBufferSize = 4 * bG711ABufferSize_rtsp;//提供足够大的缓冲区
	pAACBuffer_rtsp = (unsigned char*)malloc(bAACBufferSize * sizeof(unsigned char));
	out_len_rtsp = 0;
	if (Easy_AACEncoder_Encode(handle_rtsp, (unsigned char*)rtp_packet_buf, bG711ABufferSize_rtsp, pAACBuffer_rtsp, &out_len_rtsp) > 0)
	{
		string str = string((char*)pAACBuffer_rtsp, out_len_rtsp);
		string rtpOut = rtpHead + str;
		rtppack->assign(rtpOut.c_str(), rtppack->offset + out_len_rtsp);
	}
	else
	{
		return false;
	}


    //rtp数据开始部分
    uint8_t *ptr = (uint8_t *) rtppack->data() + rtppack->offset;
    //rtp数据末尾
    const uint8_t *end = (uint8_t *) rtppack->data() + rtppack->size();

    ////首2字节表示Au-Header的个数，单位bit，所以除以16得到Au-Header个数
    //const uint16_t au_header_count = ((ptr[0] << 8) | ptr[1]) >> 4;
    ////忽略Au-Header区
    //ptr += 2 + au_header_count * 2;

    while (ptr < end) {
        auto size = (uint32_t) (end - ptr);
        if (size > AAC_MAX_FRAME_SIZE) {
            size = AAC_MAX_FRAME_SIZE;
        }
        if (_frame->size() + size > AAC_MAX_FRAME_SIZE) {
            //数据太多了，先清空
            flushData();
        }
        //追加aac数据
        _frame->_buffer.append((char *) ptr, size);
        _frame->_dts = rtppack->timeStamp;
        ptr += size;
    }

    if (rtppack->mark) {
        //最后一个rtp分片
        flushData();
    }
    return false;
}

void AACRtpTranscodeDecoder::flushData() {
    if (_frame->_buffer.empty()) {
        //没有有效数据
        return;
    }
	_frame->_prefix_size = 7;
    RtpCodec::inputFrame(_frame);
    _frame = obtainFrame();
}


}//namespace mediakit



