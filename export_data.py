import mysql.connector
import pandas as pd

# Connect to your database
conn = mysql.connector.connect(
    host='localhost',
    database='ai_tourism_planner',
    user='root',
    password=''
)

# Export user-item interactions
query = """
SELECT user_id, package_id, 
    CASE 
        WHEN interaction_type = 'booked' THEN 5.0
        WHEN interaction_type = 'wishlisted' THEN 4.0
        WHEN interaction_type = 'viewed' THEN 1.0
        WHEN interaction_type = 'searched' THEN 0.5
    END as rating
FROM user_destination_ratings
WHERE rating > 0
"""

df = pd.read_sql(query, conn)
df.to_csv('training_data.csv', index=False)
print(f"Exported {len(df)} interactions to training_data.csv")
print(df.head())

conn.close()