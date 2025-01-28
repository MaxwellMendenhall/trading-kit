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
	public class BollingerEma : Strategy
	{
		#region Break Even Vars
		// Long BE
		private double LongTs;
		private double LongCloseValue = 0.0;
		private bool LongTrackPosition = false;
		
		//Short BE
		private double ShortTs;
		private double ShortCloseValue = 0.0;
		private bool ShortTrackPosition = false;
		#endregion
		
		private EMA shortEma;
		private EMA longEma;
		private Bollinger longBol;
		private Bollinger shortBol;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Long if price crosses over bollinger midline & > ema. Short if price crosses under bollinger midline & < ema.";
				Name										= "BollingerEma";
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
				
				// General Properties
				UseShorts = false;
				UseLongs = false;
				UseProfitTarget = false;
				
				// Long Conditions
				LongEmaPeriod = 14;
				LongBollingerPeriod = 14;
				LongBollingerStdDev = 2;
				
				LongStopLoss = 50;
				LongRiskReward = risk_to_reward.OneToTwo;
				LongTrailingStopValue					= 200;
				LongMoveTrailStop					= 150;
				
				// Short Conditions
				ShortEmaPeriod = 14;
				ShortBollingerPeriod = 14;
				ShortBollingerStdDev = 2;
				
				ShortStopLoss = 50;
				ShortRiskReward = risk_to_reward.OneToTwo;
				ShortTrailingStopValue					= 200;
				ShortMoveTrailStop					= 150;
			}
			else if (State == State.Configure)
			{
				longEma = EMA(LongEmaPeriod);
				longBol = Bollinger(LongBollingerStdDev, LongBollingerPeriod);
				
				shortEma = EMA(ShortEmaPeriod);
				shortBol = Bollinger(ShortBollingerStdDev, ShortBollingerPeriod);
			}
		}

		#region Trade Logic
		protected override void OnBarUpdate()
		{
			//Add your custom strategy logic here.
			if ( CurrentBars[0] < BarsRequiredToTrade ) return;
			
			TradeLogic();
		}
		
		private void TradeLogic()
		{
			OrderHandling();
			
			Conditions c = SearchForConditions();
			
			if ( c.LongCondition ) {
				EnterLong("BollingerEMA Long");
				SetStopLoss(CalculationMode.Ticks, LongStopLoss);
				if ( UseProfitTarget ) SetProfitTarget(CalculationMode.Ticks, LongStopLoss * GetRiskRatio(LongRiskReward));
			}
			
			if ( c.ShortCondition ) {
				EnterShort("BollingerEMA Short");
				SetStopLoss(CalculationMode.Ticks, ShortStopLoss);
				if ( UseProfitTarget ) SetProfitTarget(CalculationMode.Ticks, ShortStopLoss * GetRiskRatio(ShortRiskReward));
			}
		}
		
		private void OrderHandling()
		{
			if ( Position.MarketPosition == MarketPosition.Flat )
			{
				
			}
			
			if ( Position.MarketPosition == MarketPosition.Long )
			{
				// Move order to break even after # of ticks occurred
				if ( Close[0] > Position.AveragePrice + LongMoveTrailStop * TickSize && LongTrackPosition == false ) {
					SetStopLoss(CalculationMode.Price, Position.AveragePrice - 0 * TickSize);
					LongCloseValue = Close[0];
					LongTrackPosition = true;
					LongTs = LongTrailingStopValue;
				}
				
				if ( CrossBelow(longBol.Lower, Close, 1)) { 
					ExitLong();
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
				
				if ( CrossAbove(shortBol.Upper, Close, 1)) { 
					ExitShort();
				}
			}
		}
		
		private Conditions SearchForConditions()
		{
			
			bool longCondition = UseLongs && CrossAbove(longBol.Middle, Close, 1) && Close[0] > longEma[0];
			bool shortCondition = UseShorts && CrossBelow(shortBol.Middle, Close, 1) && Close[0] < shortEma[0];
			
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
		
		#region Properties
		
		#region Gen props
		[NinjaScriptProperty]
		[Display(Name = "Use Longs", Order = 1, GroupName = "General Properties")]
		public bool UseLongs
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Shorts", Order = 2, GroupName = "General Properties")]
		public bool UseShorts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Use Profit Target", Order = 3, GroupName = "General Properties")]
		public bool UseProfitTarget
		{ get; set; }
		#endregion
		
		#region Long props
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Long Stop Loss", Order = 1, GroupName = "Long Properties")]
		public int LongStopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Long Risk/Reward Ratio", Order = 2, GroupName = "Long Properties")]
   		public risk_to_reward LongRiskReward { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Trailing Stop Value", Order=3, GroupName="Long Properties")]
		public int LongTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Move Trail Stop", Order=4, GroupName="Long Properties")]
		public int LongMoveTrailStop
		{ get; set; }
		#endregion
		
		#region Short props
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Short Stop Loss", Order = 1, GroupName = "Short Properties")]
		public int ShortStopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Short Risk/Reward Ratio", Order = 2, GroupName = "Short Properties")]
   		public risk_to_reward ShortRiskReward { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Short Trailing Stop Value", Order=3, GroupName="Short Properties")]
		public int ShortTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Short Move Trail Stop", Order=4, GroupName="Short Properties")]
		public int ShortMoveTrailStop
		{ get; set; }
		#endregion
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Long EMA Period", Order = 1, GroupName = "Long")]
		public int LongEmaPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Long Bollinger Period", Order = 2, GroupName = "Long")]
		public int LongBollingerPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Long Bollinger Std Dev", Order = 3, GroupName = "Long")]
		public double LongBollingerStdDev
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Short EMA Period", Order = 1, GroupName = "Short")]
		public int ShortEmaPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Short Bollinger Period", Order = 2, GroupName = "Short")]
		public int ShortBollingerPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name = "Short Bollinger Std Dev", Order = 3, GroupName = "Short")]
		public double ShortBollingerStdDev
		{ get; set; }
		
		#endregion
		
		#region Helpful Functions
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
