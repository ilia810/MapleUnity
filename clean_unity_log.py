import pyperclip
import sys
import re
import argparse

def clean_unity_log(log_text, remove_duplicates=True):
    lines = log_text.split('\n')
    output_lines = []
    in_message = True
    seen_messages = set()  # Track unique messages
    
    for line in lines:
        line = line.rstrip('\r')  # Remove carriage returns
        
        # Check if this line is a stack trace line
        # Stack trace lines typically start with spaces and contain "at " or start with "UnityEngine.Debug:"
        if (line.startswith('UnityEngine.Debug:') or 
            (line.strip() and line[0] == ' ' and ' at ' in line) or
            re.match(r'^\s+at\s+', line)):
            in_message = False
        elif line == '' or not line.strip():
            # Empty line marks the end of a log entry
            in_message = True
        elif in_message and line.strip():
            # Add non-empty message lines
            if remove_duplicates:
                if line not in seen_messages:
                    output_lines.append(line)
                    seen_messages.add(line)
            else:
                output_lines.append(line)
    
    return '\n'.join(output_lines)

def main():
    parser = argparse.ArgumentParser(description='Clean Unity console logs from clipboard')
    parser.add_argument('-d', '--keep-duplicates', action='store_true', 
                        help='Keep duplicate messages (default: remove duplicates)')
    args = parser.parse_args()
    
    try:
        # Get clipboard content
        clipboard_content = pyperclip.paste()
        
        if not clipboard_content:
            print("Clipboard is empty!")
            return
        
        # Clean the log
        cleaned_log = clean_unity_log(clipboard_content, remove_duplicates=not args.keep_duplicates)
        
        # Put back to clipboard
        pyperclip.copy(cleaned_log)
        
        print("Unity log messages extracted and copied to clipboard!")
        
        # Show preview of extracted messages
        preview_lines = cleaned_log.split('\n')[:10]
        if preview_lines:
            message_count = len(cleaned_log.split('\n'))
            if args.keep_duplicates:
                print(f"\nExtracted {message_count} messages (duplicates kept). Preview:")
            else:
                print(f"\nExtracted {message_count} unique messages. Preview:")
            for line in preview_lines:
                if line:
                    print(f"  {line[:80]}{'...' if len(line) > 80 else ''}")
        
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()