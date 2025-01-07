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
using NinjaTrader.NinjaScript.Indicators.LizardIndicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.MaxBots
{
	public class TrendAholic : Strategy
	{
		
		private amaCurrentDayVWAP vwap;
		private amaGaussianFilter Gauss;
		private Order entryOrder;
		private Order stopOrder;
		private Order targetOrder;
		private LiveOrders liveOrders;
		private List<Order> activeOrders;
		
		// Long BE
		private double LongTs;
		private double LongCloseValue = 0.0;
		private bool LongTrackPosition = false;
		
		//Short BE
		private double ShortTs;
		private double ShortCloseValue = 0.0;
		private bool ShortTrackPosition = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				
				Description									= @"Best trend following strategy.";
				Name										= "TrendAholic";
				Calculate									= Calculate.OnEachTick;
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
				IsUnmanaged = false;
				showIndicators = true;
				Period = 50;
				Poles = 3;
				contracts = 1;
				stopLoss = 500;
				RiskReward = risk_to_reward.OneToTwo;
				
				LongTrailingStopValue					= 200;
				LongMoveTrailStop					= 150;
				
				ShortTrailingStopValue					= 200;
				ShortMoveTrailStop					= 150;
			}
			else if (State == State.Configure)
			{
				
				liveOrders = ResetOrders();
				activeOrders = new List<Order>();
				
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, stopLoss * GetRiskRatio(RiskReward));
				
			}
			else if (State == State.DataLoaded)
			{
				vwap = amaCurrentDayVWAP(
					amaSessionTypeVWAPD.Full_Session, 
					amaBandTypeVWAPD.Standard_Deviation, 
					amaTimeZonesVWAPD.Exchange_Time, 
					DateTime.Parse( "02:33" , System.Globalization.CultureInfo.InvariantCulture ).TimeOfDay.ToString(), 
					DateTime.Parse( "02:33" , System.Globalization.CultureInfo.InvariantCulture ).TimeOfDay.ToString(),
					1.0, 2.0, 3.0, 1.0, 2.0, 3.0);
				Gauss = amaGaussianFilter(Close, Poles, Period);

				if (showIndicators)
				{
					AddChartIndicator(vwap);
					AddChartIndicator(Gauss);
					
				}
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < BarsRequiredToTrade || vwap.SessionVWAP[0] == 0)
				return;
			
			TradeLogic();
		}
		
		#region Trade Logic
		private void TradeLogic()
		{
			OrderHandling();
			
			Conditions c = SearchForConditions();
			
			if(c.LongCondition)
			{
				EnterLong("Trendy Long");
				
				LongTrackPosition = false;
			} 
			
			if (c.ShortCondition)
			{		
				EnterShort("Trendy Short");
				
				ShortTrackPosition = false;
			}
		}
		
		private void OrderHandling()
		{
			if ( Position.MarketPosition == MarketPosition.Flat )
			{
				
			}
			
			if ( Position.MarketPosition == MarketPosition.Long )
			{
				if ( CrossBelow(Gauss, vwap.UpperBand1, 1) ) {
					ExitLong();
				}
				
				if ( CrossBelow(Gauss, vwap.SessionVWAP, 1) ) {
					ExitLong();
				}
				
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
				if ( CrossAbove(Gauss, vwap.LowerBand1, 1) ) {
					ExitShort();
				}
				
				if ( CrossAbove(Gauss, vwap.SessionVWAP, 1) ) {
					ExitShort();
				}
				
				// Move order to break even after # of ticks occurred
				if ( Close[0] < Position.AveragePrice - ShortMoveTrailStop * TickSize && ShortTrackPosition == false ) {
					SetStopLoss(CalculationMode.Price, Position.AveragePrice + 0 * TickSize);
					ShortCloseValue = Close[0];
					ShortTrackPosition = true;
					ShortTs = ShortTrailingStopValue;
				}
			}
		}
		
		private Conditions SearchForConditions()
		{
			
			bool LongCondition = Close[0] >  vwap.SessionVWAP[0] && liveOrders.EntryOrder == null && CrossAbove(Gauss, vwap.SessionVWAP, 1);
			bool ShortCondition = Close[0] < vwap.SessionVWAP[0] && liveOrders.EntryOrder == null && CrossBelow(Gauss, vwap.SessionVWAP, 1);
			
			return new Conditions
			{
				LongCondition = LongCondition,
				ShortCondition = ShortCondition
			};
		}
		#endregion
		
		#region OnOrderUpdate and OnExecutionUpdate
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			if(orderState == OrderState.Filled && liveOrders.EntryOrder == null && liveOrders.TargetOrder == null && liveOrders.StopOrder == null)
			{	
				
				liveOrders.EntryOrder = order;
				
				if (order.IsLong) 
				{

				}
				
				if (order.IsShort)
				{

				}
				
				activeOrders.Add(liveOrders.EntryOrder);
				activeOrders.Add(liveOrders.StopOrder);
				activeOrders.Add(liveOrders.TargetOrder);
			}
			else if (orderState == OrderState.Filled && liveOrders.EntryOrder != null)
			{
				liveOrders = ResetOrders();
				
				foreach (Order ao in activeOrders)
				{
				    if (ao != null && ao.OrderState == OrderState.Working)
				    {
				        CancelOrder(ao);
				    }
				}
				activeOrders.Clear();
			}
		}
		#endregion
		
		#region Helpful Fnctions
		private LiveOrders ResetOrders()
		{
			
			return new LiveOrders
			{
				EntryOrder = null,
				StopOrder = null,
				TargetOrder = null,
			};
		}
		
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
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name = "Show Indicators", Order = 1, GroupName = "General Properties")]
		public bool showIndicators
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Period", Order = 2, GroupName = "General Properties")]
		public int Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Poles", Order = 3, GroupName = "General Properties")]
		public int Poles
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Contract Count", Order = 5, GroupName = "General Properties")]
		public int contracts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Stop Loss", Order = 7, GroupName = "General Properties")]
		public int stopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Risk/Reward Ratio", Order = 8, GroupName = "General Properties")]
   		public risk_to_reward RiskReward { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trailing Stop Value", Order=30, GroupName="Long Parameters")]
		public int LongTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Move Trail Stop", Order=40, GroupName="Long Parameters")]
		public int LongMoveTrailStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trailing Stop Value", Order=30, GroupName="Short Parameters")]
		public int ShortTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Move Trail Stop", Order=40, GroupName="Short Parameters")]
		public int ShortMoveTrailStop
		{ get; set; }

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
	}
	
	#region Classes
	public class LiveOrders
	{
		public Order EntryOrder { get; set; }
		public Order StopOrder { get; set; }
		public Order TargetOrder { get; set; }
	}
	#endregion
	
}
