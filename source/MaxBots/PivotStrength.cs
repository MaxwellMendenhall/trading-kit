#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.MaxBots
{
	public class PivotStrength : Strategy
	{
		
		#region Vars
		private RSI Rsi;
		// First Resistance (R1), First Support (S1), Second Support (S2), Second Resistance (R2), Third Resistance (R3), Third Support (S3)
		private double S1Upper, S1Lower, S2Upper, S2Lower, S3Upper, S3Lower;
		private double R1Upper, R1Lower, R2Upper, R2Lower, R3Upper, R3Lower;
		private PriorDayOHLC PriorDayOHLC;
		
		// Long BE
		private double LongTs;
		private double LongCloseValue = 0.0;
		private bool LongTrackPosition = false;
		
		//Short BE
		private double ShortTs;
		private double ShortCloseValue = 0.0;
		private bool ShortTrackPosition = false;
		#endregion
		
		#region OnStateChange
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Strategy that includes RSI and Pivot points to detect possible reversals.";
				Name										= "PivotStrength";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				// Gen Properties
				RsiPeriod = 14;
				RsiSmooth = 3;
				UseLongs = false;
				UseShorts = false;
				
				// Long Params
				LongStopLoss = 50;
				LongRsiThreshold = 2;
				LongSupportThreshold = 2;
				LongRiskReward = risk_to_reward.OneToTwo;
				LongTrailingStopValue					= 200;
				LongMoveTrailStop					= 150;
				
				// ShortParams
				ShortStopLoss = 50;
				ShortRsiThreshold = 2;
				ShortSupportThreshold = 2;
				ShortRiskReward = risk_to_reward.OneToTwo;
				ShortTrailingStopValue					= 200;
				ShortMoveTrailStop					= 150;
			}
			else if (State == State.Configure)
			{
				Rsi = RSI(RsiPeriod, RsiSmooth);
				PriorDayOHLC = PriorDayOHLC();
				
				AddChartIndicator(Rsi);
			}
			else if (State == State.DataLoaded)
			{
			}
		}
		#endregion

		#region TradeLogic
		protected override void OnBarUpdate()
		{
			if ( CurrentBars[0] < BarsRequiredToTrade ) return;
			
			TradeLogic();
		}
		
		private void TradeLogic() {
			OrderHandling();
			
			Conditions c = SearchForConditions();
			
			if ( c.LongCondition) {
				EnterLong("Pivot Strenth Long");
				SetProfitTarget(CalculationMode.Ticks, LongStopLoss * GetRiskRatio(LongRiskReward));
				SetStopLoss(CalculationMode.Ticks, LongStopLoss);
			}
			
			if ( c.ShortCondition ) {
				EnterShort("Pivot Strength Short");
				SetProfitTarget(CalculationMode.Ticks, ShortStopLoss * GetRiskRatio(ShortRiskReward));
				SetStopLoss(CalculationMode.Ticks, ShortStopLoss);
			}
		}
		
		private void OrderHandling() {
			
			if ( Position.MarketPosition == MarketPosition.Long )
			{
				// Move order to break even after # of ticks occurred
				if ( Close[0] > Position.AveragePrice + LongMoveTrailStop * TickSize && LongTrackPosition == false ) {
					SetStopLoss(CalculationMode.Price, Position.AveragePrice - 0 * TickSize);
					LongCloseValue = Close[0];
					LongTrackPosition = true;
					LongTs = LongTrailingStopValue;
				}
			}
			
			if ( Position.MarketPosition == MarketPosition.Short )
			{
				// Move order to break even after # of ticks occurred
				if ( Close[0] < Position.AveragePrice - ShortMoveTrailStop * TickSize && ShortTrackPosition == false ) {
					SetStopLoss(CalculationMode.Price, Position.AveragePrice + 0 * TickSize);
					ShortCloseValue = Close[0];
					ShortTrackPosition = true;
					ShortTs = ShortTrailingStopValue;
				}
			}
			
			if ( Position.MarketPosition == MarketPosition.Flat )
			{
				
			}
		}
		
		private Conditions SearchForConditions() {
			if (Bars.IsFirstBarOfSession) {
				double PivotPoint = (PriorDayOHLC.PriorHigh[0] + PriorDayOHLC.PriorLow[0] + PriorDayOHLC.PriorClose[0])/3;
				double R1 = ( 2 * PivotPoint ) - PriorDayOHLC.PriorLow[0];
				double S1 = ( 2 * PivotPoint ) - PriorDayOHLC.PriorHigh[0];
				double R2 = PivotPoint + ( PriorDayOHLC.PriorHigh[0] - PriorDayOHLC.PriorLow[0] );
				double S2 = PivotPoint - ( PriorDayOHLC.PriorHigh[0] - PriorDayOHLC.PriorLow[0] );
				double R3 = PriorDayOHLC.PriorHigh[0] + 2 * ( PivotPoint - PriorDayOHLC.PriorLow[0] );
				double S3 = PriorDayOHLC.PriorLow[0] - 2 * ( PriorDayOHLC.PriorHigh[0] - PivotPoint );	
				
				// Draw Pivot Point Lines
				Draw.HorizontalLine(this, "R1", R1, Brushes.Blue);
				Draw.HorizontalLine(this, "S1", S1, Brushes.Yellow);
				Draw.HorizontalLine(this, "R2", R2, Brushes.Orange);
				Draw.HorizontalLine(this, "S2", S2, Brushes.White);
				Draw.HorizontalLine(this, "R3", R3, Brushes.Red);
				Draw.HorizontalLine(this, "S3", S3, Brushes.Green);
				
				// Support Bands
				S1Upper = S1 + (LongSupportThreshold * TickSize);
				S1Lower = S1 - (LongSupportThreshold * TickSize);
				S2Upper = S2 + (LongSupportThreshold * TickSize);
				S2Lower = S2 - (LongSupportThreshold * TickSize);
				S3Upper = S3 + (LongSupportThreshold * TickSize);
				S3Lower = S3 - (LongSupportThreshold * TickSize);
				
				// Res Bands
				R1Upper = R1 + (LongSupportThreshold * TickSize);
				R1Lower = R1 - (LongSupportThreshold * TickSize);
				R2Upper = R2 + (LongSupportThreshold * TickSize);
				R2Lower = R2 - (LongSupportThreshold * TickSize);
				R3Upper = R3 + (LongSupportThreshold * TickSize);
				R3Lower = R3 - (LongSupportThreshold * TickSize);
				
				// Draw Threshold regions for each Pivot Point Line
				Draw.RegionHighlightY(this, "S1 Region", true, S1Upper, S1Lower, Brushes.Blue, Brushes.Green, 20);
				Draw.RegionHighlightY(this, "S2 Region", true, S2Upper, S2Lower, Brushes.Blue, Brushes.Green, 20);
				Draw.RegionHighlightY(this, "S3 Region", true, S3Upper, S3Lower, Brushes.Blue, Brushes.Green, 20);
				Draw.RegionHighlightY(this, "R1 Region", true, R1Upper, R1Lower, Brushes.Blue, Brushes.Green, 20);
				Draw.RegionHighlightY(this, "R2 Region", true, R2Upper, R2Lower, Brushes.Blue, Brushes.Green, 20);
				Draw.RegionHighlightY(this, "R3 Region", true, R3Upper, R3Lower, Brushes.Blue, Brushes.Green, 20);
				
			}
			
			bool longCondition = UseLongs && Rsi[0] < 30 + LongRsiThreshold && Position.MarketPosition == MarketPosition.Flat
				&& (
						(Close[0] <= S1Upper && Close[0] >= S1Lower) ||
						(Close[0] <= S2Upper && Close[0] >= S2Lower) ||
						(Close[0] <= S3Upper && Close[0] >= S3Lower)
					);
			bool shortCondition = UseShorts && Rsi[0] > 70 - ShortRsiThreshold && Position.MarketPosition == MarketPosition.Flat
				&& (
						(Close[0] <= R1Upper && Close[0] >= R1Lower) ||
						(Close[0] <= R2Upper && Close[0] >= R2Lower) ||
						(Close[0] <= R3Upper && Close[0] >= R3Lower)
					);
			
			
			return new Conditions
			{
				LongCondition = longCondition,
				ShortCondition = shortCondition
			};
		}
		#endregion
		
		#region Classes
		private class Conditions
		{
			public bool LongCondition { get; set; }
			public bool ShortCondition { get; set; }
		}
		#endregion
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Use Longs", Order = 1, GroupName = "General Properties")]
		public bool UseLongs
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Shorts", Order = 2, GroupName = "General Properties")]
		public bool UseShorts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Rsi Period", Order = 3, GroupName = "General Properties")]
		public int RsiPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Rsi Smooth", Order = 1, GroupName = "General Properties")]
		public int RsiSmooth
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Long Stop Loss", Order = 1, GroupName = "Long Properties")]
		public double LongStopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Long Rsi Threshold", Order = 2, GroupName = "Long Properties")]
		public double LongRsiThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Long Risk/Reward Ratio", Order = 3, GroupName = "Long Properties")]
   		public risk_to_reward LongRiskReward { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Long Support Threshold", Order = 4, GroupName = "Long Properties")]
		public double LongSupportThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Trailing Stop Value", Order=30, GroupName="Long Properties")]
		public int LongTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Move Trail Stop", Order=40, GroupName="Long Properties")]
		public int LongMoveTrailStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Short Stop Loss", Order = 1, GroupName = "Short Properties")]
		public double ShortStopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Short Rsi Threshold", Order = 2, GroupName = "Short Properties")]
		public double ShortRsiThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Short Support Threshold", Order = 3, GroupName = "Short Properties")]
		public double ShortSupportThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Short Risk/Reward Ratio", Order = 4, GroupName = "Short Properties")]
   		public risk_to_reward ShortRiskReward { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Short Trailing Stop Value", Order=5, GroupName="Short Properties")]
		public int ShortTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Short Move Trail Stop", Order=6, GroupName="Short Properties")]
		public int ShortMoveTrailStop
		{ get; set; }
		#endregion
		
		#region Classes 
		public enum risk_to_reward
		{
			[Description( "1/0.5" )]
			OneToHalf,
			[Description( "1/1" )]
			OneToOne,
			[Description( "1/2" )]
			OneToTwo,
			[Description( "1/3" )]
			OneToThree,
			[Description( "1/4" )]
			OneToFour,
			[Description( "1/5" )]
			OneToFive,
			[Description( "1/6" )]
			OneToSix,
			[Description( "1/7" )]
			OneToSeven,
			[Description( "1/8" )]
			OneToEight,
			[Description( "1/9" )]
			OneToNine,
			[Description( "1/10" )]
			OneToTen
		}
		#endregion
		
		#region Helpful Methods
		private double GetRiskRatio( risk_to_reward revards)
		{	
			double ratio = 1;

			switch ( revards )
			{
				case risk_to_reward.OneToHalf:
					ratio = 0.5;
					break;
				case risk_to_reward.OneToOne:
					ratio = 1;
					break;
				case risk_to_reward.OneToTwo:
					ratio = 2;
					break;
				case risk_to_reward.OneToThree:
					ratio = 3;
					break;
				case risk_to_reward.OneToFour:
					ratio = 4;
					break;
				case risk_to_reward.OneToFive:
					ratio = 5;
					break;
				case risk_to_reward.OneToSix:
					ratio = 6;
					break;
				case risk_to_reward.OneToSeven:
					ratio = 7;
					break;
				case risk_to_reward.OneToEight:
					ratio = 8;
					break;
				case risk_to_reward.OneToNine:
					ratio = 9;
					break;
				case risk_to_reward.OneToTen:
					ratio = 10;
					break;
			}

			return ratio;
		}
		#endregion
	
	}
	
}
