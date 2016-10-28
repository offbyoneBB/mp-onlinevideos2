using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputStreamSourceFilter.H264
{
  public class SPSUnit
  {
    BitReader bitReader;
    uint pic_width_in_mbm_minus1;
    uint pic_height_in_map_minus1;

    uint frame_crop_left_offset;
    uint frame_crop_right_offset;
    uint frame_crop_top_offset;
    uint frame_crop_bottom_offset;

    uint frame_mbs_only_flag;

    public SPSUnit(byte[] sps)
    {
      bitReader = new BitReader(sps, 8);

      uint profile = bitReader.ReadBits(8);

      //constraint_flags
      for (int i = 0; i < 5; i++)
        bitReader.ReadBit();

      uint reserved_zero_bits = bitReader.ReadBits(3);

      uint level_idc = bitReader.ReadBits(8);
      uint sps_id = bitReader.ReadExponentialGolombCode();

      if (profile == 100 || profile == 110 || profile == 122 || profile == 244 || profile == 44 || profile == 83 || profile == 86 || profile == 118)
      {
        uint chroma_format_idc = bitReader.ReadExponentialGolombCode();
        if (chroma_format_idc == 3)
        {
          uint separate_colour_plane_flag = bitReader.ReadBit();
        }
        uint bit_depth_luma_minus8 = bitReader.ReadExponentialGolombCode();
        uint bit_depth_chroma_minus8 = bitReader.ReadExponentialGolombCode();
        uint qpprime = bitReader.ReadBit();
        bool seq_scaling_matrix_present = bitReader.ReadBit() == 1;
        if (seq_scaling_matrix_present)
        {
          int max = chroma_format_idc != 3 ? 8 : 12;
          for (int i = 0; i < max; i++)
          {
            bitReader.ReadBit();
          }
        }
      }

      uint log2_max_frame_num_minus4 = bitReader.ReadExponentialGolombCode();
      uint pic_order_cnt_type = bitReader.ReadExponentialGolombCode();
      if (pic_order_cnt_type == 0)
      {
        uint log2_max_pic_order_cnt_lsb_minus4 = bitReader.ReadExponentialGolombCode();
      }
      else if (pic_order_cnt_type == 1)
      {
        uint delta_pic_order_always_zero_flag = bitReader.ReadBit();
        int offset_for_non_ref_pic = bitReader.ReadSE();
        int offset_for_top_to_bottom_field = bitReader.ReadSE();
        uint num_ref_frames = bitReader.ReadExponentialGolombCode();
        for (int i = 0; i < num_ref_frames; i++)
          bitReader.ReadSE();
      }

      uint max_num_ref_frames = bitReader.ReadExponentialGolombCode();
      uint gaps_in_frame_num_allowed_flag = bitReader.ReadBit();
      pic_width_in_mbm_minus1 = bitReader.ReadExponentialGolombCode();
      pic_height_in_map_minus1 = bitReader.ReadExponentialGolombCode();
      frame_mbs_only_flag = bitReader.ReadBit();
      if (frame_mbs_only_flag == 0)
      {
        uint mbs_adaptive_frame_field_flag = bitReader.ReadBit();
      }
      uint direct_8x8_inference_flag = bitReader.ReadBit();
      bool frame_cropping_flag = bitReader.ReadBit() == 1;
      if (frame_cropping_flag)
      {
        frame_crop_left_offset = bitReader.ReadExponentialGolombCode();
        frame_crop_right_offset = bitReader.ReadExponentialGolombCode();
        frame_crop_top_offset = bitReader.ReadExponentialGolombCode();
        frame_crop_bottom_offset = bitReader.ReadExponentialGolombCode();
      }

      bool vui_parameters_present_flag = bitReader.ReadBit() == 1;
    }

    public int Width()
    {
      uint width = (pic_width_in_mbm_minus1 + 1) * 16;
      width -= frame_crop_left_offset * 2;
      width -= frame_crop_right_offset * 2;
      return (int)width;
    }

    public int Height()
    {
      uint height = (2 - frame_mbs_only_flag) * (pic_height_in_map_minus1 + 1) * 16;
      height -= frame_crop_top_offset * 2;
      height -= frame_crop_bottom_offset * 2;
      return (int)height;
    }
  }
}