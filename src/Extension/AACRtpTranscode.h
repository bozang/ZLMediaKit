﻿/*
 * Copyright (c) 2016 The ZLMediaKit project authors. All Rights Reserved.
 *
 * This file is part of ZLMediaKit(https://github.com/xiongziliang/ZLMediaKit).
 *
 * Use of this source code is governed by MIT license that can be found in the
 * LICENSE file in the root of the source tree. All contributing project authors
 * may be found in the AUTHORS file in the root of the source tree.
 */

#ifndef ZLMEDIAKIT_AACRTPTRANSCODECODEC_H
#define ZLMEDIAKIT_AACRTPTRANSCODECODEC_H

#include "Rtsp/RtpCodec.h"
#include "Extension/AAC.h"
#include "EasyAACEncoderAPI.h"
namespace mediakit{
/**
 * aac rtp转adts类
 */
class AACRtpTranscodeDecoder : public RtpCodec , public ResourcePoolHelper<AACFrame> {
public:
    typedef std::shared_ptr<AACRtpTranscodeDecoder> Ptr;
	AACRtpTranscodeDecoder(const Track::Ptr &track);
    ~AACRtpTranscodeDecoder() {}
	/*AACRtpTranscodeDecoder();*/
    /**
     * 输入rtp并解码
     * @param rtp rtp数据包
     * @param key_pos 此参数内部强制转换为false,请忽略之
     */
    bool inputRtp(const RtpPacket::Ptr &rtp, bool key_pos = false) override;

    CodecId getCodecId() const override {
        return CodecAAC;
    }

//protected:
//	AACRtpTranscodeDecoder();

private:
    AACFrame::Ptr obtainFrame();
    void flushData();

private:
    AACFrame::Ptr _frame;
    string _aac_cfg;
	void* handle_rtsp;
	int bG711ABufferSize_rtsp;
	unsigned int out_len_rtsp;
	unsigned char *pAACBuffer_rtsp;
};


///**
// * aac adts转rtp类
// */
//class AACRtpTranscodeEncoder : public AACRtpTranscodeDecoder, public RtpInfo {
//public:
//    typedef std::shared_ptr<AACRtpTranscodeEncoder> Ptr;
//
//    /**
//     * @param ui32Ssrc ssrc
//     * @param ui32MtuSize mtu 大小
//     * @param ui32SampleRate 采样率
//     * @param ui8PayloadType pt类型
//     * @param ui8Interleaved rtsp interleaved 值
//     */
//	AACRtpTranscodeEncoder(uint32_t ui32Ssrc,
//                  uint32_t ui32MtuSize,
//                  uint32_t ui32SampleRate,
//                  uint8_t ui8PayloadType = 97,
//                  uint8_t ui8Interleaved = TrackAudio * 2);
//    ~AACRtpTranscodeEncoder() {}
//
//    /**
//     * 输入aac 数据，必须带dats头
//     * @param frame 带dats头的aac数据
//     */
//    void inputFrame(const Frame::Ptr &frame) override;
//
//private:
//    void makeAACRtp(const void *pData, unsigned int uiLen, bool bMark, uint32_t uiStamp);
//
//private:
//    unsigned char _aucSectionBuf[1600];
//};

}//namespace mediakit

#endif //ZLMEDIAKIT_AACRTPCODEC_H