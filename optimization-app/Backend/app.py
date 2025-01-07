from fastapi import FastAPI, UploadFile, File
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from fastapi.responses import JSONResponse
from typing import List
import uvicorn
import pandas as pd
from util.env import FRONTEND

app = FastAPI()

origins = [
    FRONTEND
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class FileData(BaseModel):
    files: List[UploadFile]
    
class TradingData(BaseModel):
    instrument: str
    parameters: str
    total_net_profit: float
    profit_factor: float
    max_drawdown: float
    num_trades: float
    percent_profitable: float
    
class ResponseData(BaseModel):
    data: List[TradingData]
    
@app.post("/submit", response_model=ResponseData)
async def submit_form(files: List[UploadFile] = File(...)):
    print(files)
    trading_data_list: List = []
    
    for i in files:
        
        if i.content_type != "text/csv":
            return JSONResponse(content={"error": f"Invalid file type for {i.filename}. Please upload a CSV file."}, status_code=400)
        
        try: 
            df = pd.read_csv(i.file)
            df['Instrument'] = df['Instrument'].fillna('')
            print(df)
        except Exception as e:
            return JSONResponse(content={"error": f"Error reading CSV file {i.filename}: {str(e)}"}, status_code=400)
        
        for _, row in df.iterrows():
            trading_data = TradingData(
                instrument=row.get('Instrument', ''),
                parameters=str(row.get('Parameters', '')),
                total_net_profit=row.get('Total net profit', 0.0),
                profit_factor=row.get('Profit factor', 0.0),
                max_drawdown=row.get('Max. drawdown', 0.0),
                num_trades=row.get('Total # of trades', 0.0),
                percent_profitable=row.get('Percent profitable', 0.0)
            )
            trading_data_list.append(trading_data)
            
    print(trading_data_list)
    
    return ResponseData(data=trading_data_list)

@app.get("/")
async def root():
    return {"message": "Welcome to the FastAPI application!"}
    
if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)